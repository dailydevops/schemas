namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;

/// <summary>
/// One run of the pipeline (Structures 5.1, design #146 DP-5): read the documents, compose them, build the components, resolve the
/// references, check the constraints, and compile the content models. Every phase collects its errors; the run ends with a schema
/// set only when there is none.
/// </summary>
/// <remarks>
/// <para>
/// A run is not reusable and not thread-safe; <see cref="SchemaCompiler"/> creates one per call. Components are built in the order
/// that their dependencies demand (<see cref="BuildContext"/>): simple types, attribute declarations, attribute groups, complex
/// types, then element declarations, model group definitions, and identity constraints, then the anonymous types of the elements.
/// Everything that is named is registered; the resolver binds the by-name references once all of it exists.
/// </para>
/// <para>
/// Limitations: the strict XSD 1.0 gate stops the run before any component is built (design #146 DP-16); conditional inclusion
/// (<c>vc:</c> attributes) is not applied in this run, because it is part of the XSD 1.1 pipeline (XSD-78, #360); substitution group
/// affiliation (<c>substitutionGroup</c>, XSD-12), notation declarations, <c>openContent</c>, assertions, type alternatives, and the
/// subsumption check of XSD 1.1 are not built or checked; the limit of external resources applies to each root document alone.
/// </para>
/// </remarks>
internal sealed class CompilationRun
{
    private static readonly SchemaNamespaceScope NoNamespace = new(string.Empty, false);

    private readonly XsdVersion _version;
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;
    private readonly ValidationIssueCollector _collector;
    private readonly XsdIssueReporter _reporter;
    private readonly ComponentRegistry _registry;
    private readonly BuildContext _context;
    private readonly ComponentResolver _componentResolver;
    private readonly TypeDerivationCheck _derivation;
    private readonly DeterminismChecker _determinism;
    private readonly List<ModelGroupDefinition> _definitions = [];
    private readonly List<IdentityConstraintDefinition> _constraints = [];

    internal CompilationRun(XsdVersion version, IResourceResolver resolver, ValidationLimits limits)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(limits);

        _version = version;
        _resolver = resolver;
        _limits = limits;
        _collector = new ValidationIssueCollector(limits);
        _reporter = new XsdIssueReporter(_collector);
        _registry = new ComponentRegistry(_reporter);
        XsdBuiltInTypes.Register(_registry, version);
        _context = new BuildContext(version, _registry, _reporter);
        _componentResolver = new ComponentResolver(version);
        _derivation = new TypeDerivationCheck(version);
        _determinism = new DeterminismChecker();
    }

    /// <summary>
    /// Gets the steps the run composes: the real implementations of the seams of the contract. The element and model group
    /// builders are bound to the namespace scope of a document, so the run creates one of each per unit; the types are listed here.
    /// </summary>
    internal IReadOnlyList<object> Steps =>
        [
            _componentResolver,
            _derivation,
            _determinism,
            _context.SimpleTypes,
            _context.ComplexTypes,
            _context.Attributes,
            _context.AttributeGroups,
            new ElementDeclarationBuilder(NoNamespace),
            new ModelGroupDefinitionBuilder(NoNamespace),
        ];

    /// <summary>Reads a schema document given as characters.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The parsed document.</returns>
    internal SchemaDocument Read(ReadOnlySpan<char> schema, string? baseUri) =>
        Checked(SchemaDocumentParser.Parse(schema, baseUri, _reporter));

    /// <summary>Reads a schema document given as bytes.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The parsed document.</returns>
    internal SchemaDocument Read(ReadOnlySpan<byte> schema, string? baseUri) =>
        Checked(SchemaDocumentParser.Parse(schema, baseUri, _reporter));

    /// <summary>Reads a schema document given as a <see cref="SchemaInput"/>.</summary>
    /// <param name="input">The input.</param>
    /// <returns>The parsed document.</returns>
    internal SchemaDocument Read(in SchemaInput input) =>
        input.IsBytes ? Read(input.Bytes, input.BaseUri) : Read(input.Chars, input.BaseUri);

    /// <summary>Runs every phase after reading.</summary>
    /// <param name="documents">The parsed root documents.</param>
    /// <param name="cancellationToken">The token to cancel the run.</param>
    /// <returns>The outcome.</returns>
    internal SchemaCompilation Execute(IReadOnlyList<SchemaDocument> documents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(documents);

        var units = Compose(documents, cancellationToken);
        Prepare(units);
        if (!HasGateErrors(units))
        {
            Build(units, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Resolve();
            CompileChecks.Run(_context, _derivation, _definitions, _version, _reporter);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Finish();
    }

    private SchemaDocument Checked(SchemaDocument document)
    {
        if (document.Root is null && document.ReadError is not null)
        {
            _reporter.Report(
                XsdRuleCodes.SchemaReference,
                new DocumentLocation(
                    document.BaseUri,
                    document.ReadError.Position.Line,
                    document.ReadError.Position.Column
                ),
                "The schema document '"
                    + (document.BaseUri ?? "(unnamed)")
                    + "' could not be read: "
                    + document.ReadError.Message
                    + "."
            );
        }

        return document;
    }

    // Include, import, redefine, and override of every root document; a document that two roots reach is one unit.
    private List<UnitScope> Compose(IReadOnlyList<SchemaDocument> documents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var units = new List<UnitScope>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (document.Root is null)
            {
                continue;
            }

            var set = new SchemaDocumentLoader(_resolver, _limits, _collector).Load(document, cancellationToken);
            foreach (var unit in set.Units)
            {
                var uri = unit.Document.BaseUri;
                if (
                    uri is null
                    || seen.Add(
                        uri
                            + "\u0000"
                            + unit.EffectiveNamespace
                            + (unit.Overrides is null ? string.Empty : "\u0001" + unit.Overrides.Key)
                    )
                )
                {
                    units.Add(new UnitScope(unit));
                }
            }
        }

        return units;
    }

    // Each document is prepared once, also when several units share it.
    private void Prepare(List<UnitScope> units)
    {
        var prepared = new HashSet<SchemaDocument>();
        foreach (var unit in units)
        {
            if (prepared.Add(unit.Unit.Document))
            {
                DocumentPreparation.Prepare(unit.Unit.Document.Root!, _version, _reporter);
            }
        }
    }

    // Strict 1.0 mode rejects every 1.1 construct of every document; nothing is built after that (design #146 DP-16).
    private bool HasGateErrors(List<UnitScope> units)
    {
        var gate = new ValidationIssueCollector(_limits);
        var reporter = new XsdIssueReporter(gate);
        var checkedDocuments = new HashSet<SchemaDocument>();
        foreach (var unit in units)
        {
            if (checkedDocuments.Add(unit.Unit.Document))
            {
                Strict10Constructs.Check(unit.Unit.Document.Root!, _version, reporter);
            }
        }

        var issues = gate.ToResult().Issues;
        foreach (var issue in issues)
        {
            _collector.Add(issue);
        }

        return issues.Count > 0;
    }

    private void Build(List<UnitScope> units, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _context.Index(units);
        BuildDefinitions("simpleType", cancellationToken);
        BuildDefinitions("attribute", cancellationToken);
        BuildDefinitions("attributeGroup", cancellationToken);
        foreach (var group in _context.BuiltAttributeGroups)
        {
            group.Resolve(_registry, _reporter);
        }

        BuildDefinitions("complexType", cancellationToken);
        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BuildDeclarations(unit);
        }

        while (_context.PendingElements.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pending = _context.PendingElements.Dequeue();
            var syntax = (SchemaSyntaxNode)pending.Key.InlineTypeSyntax!;
            pending.Key.BindInlineType(
                SyntaxValues.IsSchemaElement(syntax, "complexType")
                    ? _context.ComplexTypes.BuildAnonymous(syntax, pending.Value)
                    : _context.SimpleTypes.BuildAnonymous(syntax, pending.Value)
            );
        }
    }

    private void BuildDefinitions(string localName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var definition in _context.Definitions)
        {
            if (!string.Equals(definition.Node.Name.LocalName, localName, StringComparison.Ordinal))
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            switch (localName)
            {
                case "attribute":
                    _ = _context.Attributes.Ensure(definition);
                    break;
                case "attributeGroup":
                    _ = _context.AttributeGroups.Ensure(definition);
                    break;
                default:
                    _ = _context.EnsureType(definition);
                    break;
            }
        }
    }

    // The element declarations and model group definitions of a unit, and the identity constraints below its elements.
    private void BuildDeclarations(UnitScope unit)
    {
        for (var i = 0; i < unit.Unit.Components.Count; i++)
        {
            var node = unit.Unit.Components[i];
            switch (node.Name.LocalName)
            {
                case "element":
                    _context.ComplexTypes.Scan(
                        (ElementDeclaration)unit.Elements.Build(node, _registry, _reporter),
                        unit
                    );
                    break;
                case "group":
                    var definition = (ModelGroupDefinition)unit.Groups.Build(node, _registry, _reporter);
                    _definitions.Add(definition);
                    _context.ComplexTypes.Scan(definition.ModelGroup, unit);
                    break;
                default:
                    break;
            }

            AddConstraints(node, unit);
        }
    }

    private void AddConstraints(SchemaSyntaxNode node, UnitScope unit)
    {
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (
                SyntaxValues.IsSchemaElement(node, "element")
                && (
                    SyntaxValues.IsSchemaElement(child, "key")
                    || SyntaxValues.IsSchemaElement(child, "unique")
                    || SyntaxValues.IsSchemaElement(child, "keyref")
                )
            )
            {
                // The document parser reported an empty name (a name is an NCName); the factory of the definition expects a non-empty one.
                var constraint = child.FindAttribute("name") is { Length: 0 }
                    ? null
                    : IdentityConstraintDefinition.Create(child, unit.Scope, _registry, _reporter);
                if (constraint is not null)
                {
                    _constraints.Add(constraint);
                }
            }
            else
            {
                AddConstraints(child, unit);
            }
        }
    }

    private void Resolve()
    {
        _componentResolver.Resolve(_registry, _reporter);
        foreach (var component in _context.Anonymous)
        {
            for (var i = 0; i < component.ReferenceCount; i++)
            {
                var reference = component.GetReference(i);
                if (_registry.TryGet(reference.Space, reference.Name, out var target))
                {
                    component.Bind(i, target!);
                }
                else
                {
                    _reporter.Report(
                        XsdRuleLookup.SrcResolve,
                        reference.Location,
                        "No " + reference.Space + " component named '" + reference.Name + "' is declared."
                    );
                }
            }
        }

        foreach (var constraint in _constraints)
        {
            constraint.ResolveRefer(_registry, _reporter);
        }
    }

    // The set is frozen, the content models are compiled for it, and the unique particle attribution of each is checked.
    private SchemaCompilation Finish()
    {
        var set = _registry.Freeze(_version);
        var model = ValidationModel.Build(set, _registry.Components, _reporter);
        foreach (var type in _context.BuiltComplexTypes)
        {
            if (model.GetProgram(type) is { Automaton: { } automaton } program)
            {
                _ = _determinism.Check(automaton, program.Terms, model.NameOf, _reporter, type.Location);
            }
        }

        var result = _collector.ToResult();
        return result.IsValid ? new SchemaCompilation(set, model, result) : new SchemaCompilation(null, null, result);
    }
}
