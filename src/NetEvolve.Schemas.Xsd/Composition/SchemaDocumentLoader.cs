namespace NetEvolve.Schemas.Xsd.Composition;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Override;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Loads the schema documents reached by <c>include</c>, <c>import</c>, <c>redefine</c>, and <see langword="override"/> through the resolver and applies
/// the composition on the syntax model, before any component exists (design #146 DP-3).
/// </summary>
/// <remarks>
/// <para>
/// Every document is a unit of a resolved URI and an effective target namespace; a unit is loaded once, so cycles end
/// without a report (Structures 1.1, 4.2.3 treats a cyclic include as multiple inclusion). Every location goes through the
/// resolver with <see cref="ResourceKind.SchemaDocument"/>; each request counts against
/// <see cref="ValidationLimits.MaxExternalResources"/>, also a refused one, and the size of a document against
/// <see cref="ValidationLimits.MaxResourceSize"/>. A refusal, a miss, and a limit are reported with the core code at the
/// directive, the namespace rules of the directives with <c>src-include</c>, <c>src-redefine</c>, <c>src-import</c>, and
/// <c>src-override</c>.
/// </para>
/// <para>
/// Redefine is a rewrite equivalent to the transformation of Structures 1.1, 4.2.2 for both versions: the original
/// component of the redefined document keeps living under a fresh name that only the self-reference of the redefinition
/// uses, and the redefinition takes the place of the original in the redefining unit. Limitations: the loader works on the
/// syntax only, so the restriction check for a group or attribute group redefined without a self-reference, and the checks
/// of a redefined type against its original, belong to the component checks; the 1.1
/// specifics of <c>redefine</c> are not distinguished from the 1.0 rules.
/// </para>
/// <para>
/// Override is the transformation of Structures 1.1, F.2: the overridden document, and every document of its target set (the
/// documents it includes or overrides), becomes a unit of its own whose components are those of the document with the
/// matching ones replaced by the overriding nodes (see <see cref="OverrideSet"/>). A document reached under another set of
/// overriding nodes is another unit, so a document that is included or redefined and also overridden is present twice with
/// conflicting components, as Structures 1.1, 4.2.5 describes; the component registry reports the duplicate. Limitation: a
/// set that changes nothing in a document leaves it a plain unit, decided on the document and not on the documents it includes.
/// </para>
/// </remarks>
internal sealed class SchemaDocumentLoader
{
    private static readonly XsdRule SrcInclude = Rule("src-include");
    private static readonly XsdRule SrcRedefine = Rule("src-redefine");
    private static readonly XsdRule SrcExpredef = Rule("src-expredef");
    private static readonly XsdRule SrcImport = Rule("src-import");
    private static readonly XsdRule SrcOverride = Rule("src-override");
    private static readonly XsdRule SchemaReference = Rule("schema_reference");

    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;
    private readonly ValidationIssueCollector _collector;
    private readonly XsdIssueReporter _reporter;
    private readonly Dictionary<string, SchemaUnit> _byKey = new(StringComparer.Ordinal);
    private readonly List<SchemaUnit> _units = [];
    private readonly Queue<SchemaUnit> _pending = new();
    private readonly List<Redefinition> _redefinitions = [];
    private readonly Dictionary<SchemaSyntaxNode, int> _nodeIdentities = [];
    private CancellationToken _cancellationToken;
    private int _requests;
    private bool _limitReported;

    internal SchemaDocumentLoader(
        IResourceResolver resolver,
        ValidationLimits limits,
        ValidationIssueCollector collector
    )
    {
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(limits);
        Argument.ThrowIfNull(collector);

        _resolver = resolver;
        _limits = limits;
        _collector = collector;
        _reporter = new XsdIssueReporter(collector);
    }

    /// <summary>Loads the documents reached from a root document and applies include, import, and redefine.</summary>
    /// <param name="root">The parsed root document.</param>
    /// <param name="cancellationToken">The caller's cancellation token, passed to the resolver.</param>
    /// <returns>The units, the root first; no unit when the root could not be read.</returns>
    internal SchemaDocumentSet Load(SchemaDocument root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(root);

        _cancellationToken = cancellationToken;
        _byKey.Clear();
        _units.Clear();
        _pending.Clear();
        _redefinitions.Clear();
        _nodeIdentities.Clear();
        _requests = 0;
        _limitReported = false;

        if (root.Root is not null)
        {
            _ = Add(root, TargetNamespaceOf(root.Root) ?? string.Empty, root.BaseUri);
            while (_pending.Count > 0)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                Process(_pending.Dequeue());
            }

            ApplyRedefinitions();
        }

        return new SchemaDocumentSet(new List<SchemaUnit>(_units));
    }

    private static XsdRule Rule(string id)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, id, StringComparison.Ordinal))
            {
                return rule;
            }
        }

        throw new InvalidOperationException("The rule catalog has no rule " + id + ".");
    }

    private static string? TargetNamespaceOf(SchemaSyntaxNode root) => root.FindAttribute("targetNamespace");

    private static string Key(string uri, string effectiveNamespace) => uri + "\u0000" + effectiveNamespace;

    /// <summary>Gives the namespace part of the key of a unit; a unit under an override set is not the plain unit of its document.</summary>
    private static string Scope(string effectiveNamespace, OverrideSet? overrides) =>
        overrides is null ? effectiveNamespace : effectiveNamespace + "\u0001" + overrides.Key;

    private static bool IsSchemaElement(SchemaSyntaxNode node, string localName) =>
        string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
        && string.Equals(node.Name.LocalName, localName, StringComparison.Ordinal);

    private SchemaUnit Add(
        SchemaDocument document,
        string effectiveNamespace,
        string? baseUri,
        OverrideSet? overrides = null
    )
    {
        var unit = new SchemaUnit(document, document.Root!, effectiveNamespace, overrides);
        _units.Add(unit);
        _pending.Enqueue(unit);
        if (baseUri is not null)
        {
            _byKey[Key(baseUri, Scope(effectiveNamespace, overrides))] = unit;
        }

        return unit;
    }

    private void Process(SchemaUnit unit)
    {
        var root = unit.Root;
        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChildNode(i);
            if (!string.Equals(child.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
            {
                continue;
            }

            switch (child.Name.LocalName)
            {
                case "include":
                    Include(unit, child);
                    break;
                case "import":
                    Import(unit, child);
                    break;
                case "redefine":
                    Redefine(unit, child);
                    break;
                case "override":
                    Override(unit, child);
                    break;
                case "simpleType":
                case "complexType":
                case "group":
                case "attributeGroup":
                case "element":
                case "attribute":
                case "notation":
                    unit.Components.Add(unit.Overrides?.Replace(child) ?? child);
                    break;
                default:
                    break;
            }
        }
    }

    private void Include(SchemaUnit unit, SchemaSyntaxNode node)
    {
        var location = node.FindAttribute("schemaLocation");
        if (location is null)
        {
            return;
        }

        var target = Fetch(unit, node, location, unit.EffectiveNamespace, null, SrcInclude, unit.Overrides);
        if (target is not null)
        {
            unit.Included.Add(target);
        }
    }

    /// <summary>An override loads its target under the components it offers; the target set takes them on (F.2).</summary>
    private void Override(SchemaUnit unit, SchemaSyntaxNode node)
    {
        var location = node.FindAttribute("schemaLocation");
        if (location is null)
        {
            return;
        }

        var overrides = OverrideSet.Create(node, unit.Overrides, _nodeIdentities);
        var target = Fetch(unit, node, location, unit.EffectiveNamespace, null, SrcOverride, overrides);
        if (target is not null)
        {
            unit.Included.Add(target);
        }
    }

    private void Import(SchemaUnit unit, SchemaSyntaxNode node)
    {
        var imported = node.FindAttribute("namespace");
        if (imported is null && unit.EffectiveNamespace.Length > 0)
        {
            _reporter.Report(
                SrcImport,
                node.Location,
                "The import has no namespace attribute, which a schema with a target namespace must give."
            );
            return;
        }

        if (imported is not null && string.Equals(imported, unit.EffectiveNamespace, StringComparison.Ordinal))
        {
            _reporter.Report(
                SrcImport,
                node.Location,
                "The namespace '" + imported + "' of the import is the target namespace of the importing schema."
            );
            return;
        }

        var location = node.FindAttribute("schemaLocation");
        if (location is not null)
        {
            _ = Fetch(unit, node, location, imported ?? string.Empty, imported, SrcImport);
        }
    }

    private void Redefine(SchemaUnit unit, SchemaSyntaxNode node)
    {
        var location = node.FindAttribute("schemaLocation");
        if (location is null)
        {
            return;
        }

        var target = Fetch(unit, node, location, unit.EffectiveNamespace, null, SrcRedefine);
        if (target is null)
        {
            return;
        }

        unit.Included.Add(target);
        for (var i = 0; i < node.ChildCount; i++)
        {
            var written = node.GetChildNode(i);
            var child = unit.Overrides?.Replace(written) ?? written;
            var name = child.FindAttribute("name");
            if (name is null || !IsRedefinable(child))
            {
                continue;
            }

            var references = new List<SelfReference>();
            if (!CheckSelfReference(child, name, unit.EffectiveNamespace, references))
            {
                _reporter.Report(
                    SrcRedefine,
                    child.Location,
                    "The redefinition of '"
                        + name
                        + "' does not refer to the component it redefines in the way the specification demands."
                );
                continue;
            }

            unit.Components.Add(child);
            _redefinitions.Add(new Redefinition(unit, child, name, target, references));
        }
    }

    private static bool IsRedefinable(SchemaSyntaxNode node) =>
        IsSchemaElement(node, "simpleType")
        || IsSchemaElement(node, "complexType")
        || IsSchemaElement(node, "group")
        || IsSchemaElement(node, "attributeGroup");

    private SchemaUnit? Fetch(
        SchemaUnit unit,
        SchemaSyntaxNode node,
        string reference,
        string effectiveNamespace,
        string? importNamespace,
        XsdRule rule,
        OverrideSet? overrides = null
    )
    {
        var resolution = UriReference.Resolve(reference, unit.Document.BaseUri);
        if (resolution.Uri is null && resolution.Code == CoreDiagnosticCodes.ResourceReferenceInvalid)
        {
            Core(resolution.Code, node, rule, "The schema location '" + reference + "' is not a valid URI reference.");
            return null;
        }

        var scope = Scope(effectiveNamespace, overrides);
        var uri = resolution.Uri is null ? null : WithoutFragment(resolution.Uri);
        if (uri is not null && _byKey.TryGetValue(Key(uri, scope), out var known))
        {
            return known;
        }

        if (
            overrides is not null
            && uri is not null
            && _byKey.TryGetValue(Key(uri, effectiveNamespace), out var plain)
            && overrides.LeavesUnchanged(plain.Root)
        )
        {
            _byKey[Key(uri, scope)] = plain;
            return plain;
        }

        if (_requests >= _limits.MaxExternalResources)
        {
            ReportLimit(node, rule);
            return null;
        }

        _cancellationToken.ThrowIfCancellationRequested();
        _requests++;
        var request = new ResourceRequest(
            ResourceKind.SchemaDocument,
            reference,
            unit.Document.BaseUri,
            resolution.Uri,
            namespaceUri: importNamespace
        );
        var document = Retrieve(in request, uri, scope, node, rule, out var systemId, out known);
        if (known is not null)
        {
            return known;
        }

        if (document?.Root is null)
        {
            return null;
        }

        var loaded = Accept(document, effectiveNamespace, importNamespace is not null, systemId, node, rule, overrides);
        if (loaded is not null && uri is not null)
        {
            _byKey[Key(uri, scope)] = loaded;
        }

        return loaded;
    }

    private void ReportLimit(SchemaSyntaxNode node, XsdRule rule)
    {
        if (_limitReported)
        {
            return;
        }

        _limitReported = true;
        Core(
            CoreDiagnosticCodes.ExternalResourceLimitExceeded,
            node,
            rule,
            "More than " + _limits.MaxExternalResources + " schema documents are referenced."
        );
    }

    /// <summary>Checks the target namespace of a loaded document against the directive and adds it as a unit.</summary>
    private SchemaUnit? Accept(
        SchemaDocument document,
        string effectiveNamespace,
        bool exact,
        string? systemId,
        SchemaSyntaxNode node,
        XsdRule rule,
        OverrideSet? overrides
    )
    {
        var found = TargetNamespaceOf(document.Root!);
        if (NamespaceMatches(found, effectiveNamespace, exact))
        {
            if (overrides is null || !overrides.LeavesUnchanged(document.Root!))
            {
                return Add(document, effectiveNamespace, systemId, overrides);
            }

            // The override changes nothing in this document: it is the plain unit of the document.
            return systemId is not null && _byKey.TryGetValue(Key(systemId, effectiveNamespace), out var plain)
                ? plain
                : Add(document, effectiveNamespace, systemId);
        }

        _reporter.Report(
            rule,
            node.Location,
            "The target namespace '"
                + (found ?? "(none)")
                + "' of the schema document '"
                + (systemId ?? "(unnamed)")
                + "' is not '"
                + (effectiveNamespace.Length == 0 ? "(none)" : effectiveNamespace)
                + "'."
        );
        return null;
    }

    /// <summary>Asks the resolver, maps its status to the core codes, and parses what it found.</summary>
    private SchemaDocument? Retrieve(
        in ResourceRequest request,
        string? uri,
        string scope,
        SchemaSyntaxNode node,
        XsdRule rule,
        out string? systemId,
        out SchemaUnit? known
    )
    {
        systemId = uri;
        known = null;
        using var result = _resolver.Resolve(in request, _cancellationToken);
        if (result.Status != ResourceStatus.Found)
        {
            var found = result.Status == ResourceStatus.NotFound;
            Core(
                found ? CoreDiagnosticCodes.ResourceNotFound : CoreDiagnosticCodes.ResourceDenied,
                node,
                rule,
                result.Message
                    ?? "The schema document '"
                        + (uri ?? request.Reference)
                        + (found ? "' was not found." : "' was denied.")
            );
            return null;
        }

        systemId = result.SystemId is null ? uri : WithoutFragment(result.SystemId);
        return systemId is not null && _byKey.TryGetValue(Key(systemId, scope), out known)
            ? null
            : Read(in result, systemId, node, rule);
    }

    /// <summary>An import needs the exact namespace; an include or redefine accepts the namespace or none (chameleon).</summary>
    private static bool NamespaceMatches(string? found, string expected, bool exact) =>
        found is null ? !exact || expected.Length == 0 : string.Equals(found, expected, StringComparison.Ordinal);

    private static string WithoutFragment(string uri)
    {
        var hash = uri.AsSpan().IndexOf('#');
        return hash < 0 ? uri : uri.Substring(0, hash);
    }

    private void Core(DiagnosticCode code, SchemaSyntaxNode node, XsdRule rule, string message) =>
        _collector.Add(new ValidationIssue(code, IssueSeverity.Error, message, node.Location, rule.Id));

    private SchemaDocument? Read(in ResourceResult result, string? baseUri, SchemaSyntaxNode node, XsdRule rule)
    {
        var limit = _limits.MaxResourceSize;
        if (result.Stream is null)
        {
            if (result.Content.Length > limit)
            {
                ReportTooLarge(node, rule, limit);
                return null;
            }

            return Parse(result.Content.Span, baseUri, node);
        }

        using var buffer = new MemoryStream();
        if (!Copy(result.Stream, buffer, limit))
        {
            ReportTooLarge(node, rule, limit);
            return null;
        }

        return Parse(new ReadOnlySpan<byte>(buffer.GetBuffer(), 0, (int)buffer.Length), baseUri, node);
    }

    private static bool Copy(Stream source, MemoryStream destination, long limit)
    {
        if (source.CanSeek && source.Length > limit)
        {
            return false;
        }

        var rented = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            long total = 0;
            int read;
            while ((read = source.Read(rented, 0, rented.Length)) > 0)
            {
                total += read;
                if (total > limit)
                {
                    return false;
                }

                destination.Write(rented, 0, read);
            }

            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private void ReportTooLarge(SchemaSyntaxNode node, XsdRule rule, long limit) =>
        Core(
            CoreDiagnosticCodes.ResourceSizeLimitExceeded,
            node,
            rule,
            "The schema document is larger than " + limit + " bytes."
        );

    private SchemaDocument? Parse(ReadOnlySpan<byte> content, string? baseUri, SchemaSyntaxNode node)
    {
        var document = SchemaDocumentParser.Parse(content, baseUri, _reporter);
        if (document.Root is null && document.ReadError is not null)
        {
            _reporter.Report(
                SchemaReference,
                node.Location,
                "The schema document '"
                    + (baseUri ?? "(unnamed)")
                    + "' could not be read: "
                    + document.ReadError.Message
                    + " (line "
                    + document.ReadError.Position.Line
                    + ", column "
                    + document.ReadError.Position.Column
                    + ")."
            );
        }

        return document;
    }

    // Redefine: the self-references of the redefinition (Structures 1.0, 4.2.2, src-redefine).

    private static bool CheckSelfReference(
        SchemaSyntaxNode node,
        string name,
        string effectiveNamespace,
        List<SelfReference> references
    )
    {
        switch (node.Name.LocalName)
        {
            case "simpleType":
                return FindChildren(node, "restriction", "base", name, effectiveNamespace, references) == 1;
            case "complexType":
                var found = 0;
                for (var i = 0; i < node.ChildCount; i++)
                {
                    var content = node.GetChildNode(i);
                    if (IsSchemaElement(content, "complexContent") || IsSchemaElement(content, "simpleContent"))
                    {
                        found += FindChildren(content, "restriction", "base", name, effectiveNamespace, references);
                        found += FindChildren(content, "extension", "base", name, effectiveNamespace, references);
                    }
                }

                return found == 1;
            case "group":
                var before = references.Count;
                FindDescendants(node, name, effectiveNamespace, references);
                return references.Count - before <= 1
                    && (references.Count == before || HasOneOccurrence(references[references.Count - 1].Node));
            default:
                return FindChildren(node, "attributeGroup", "ref", name, effectiveNamespace, references) <= 1;
        }
    }

    private static bool HasOneOccurrence(SchemaSyntaxNode node) =>
        IsOneOrAbsent(node.FindAttribute("minOccurs")) && IsOneOrAbsent(node.FindAttribute("maxOccurs"));

    private static bool IsOneOrAbsent(string? value) =>
        value is null || string.Equals(value, "1", StringComparison.Ordinal);

    private static int FindChildren(
        SchemaSyntaxNode parent,
        string localName,
        string attribute,
        string name,
        string effectiveNamespace,
        List<SelfReference> references
    )
    {
        var count = 0;
        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildNode(i);
            if (IsSchemaElement(child, localName) && Refers(child, attribute, name, effectiveNamespace))
            {
                references.Add(new SelfReference(child, attribute));
                count++;
            }
        }

        return count;
    }

    private static void FindDescendants(
        SchemaSyntaxNode parent,
        string name,
        string effectiveNamespace,
        List<SelfReference> references
    )
    {
        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildNode(i);
            if (IsSchemaElement(child, "group") && Refers(child, "ref", name, effectiveNamespace))
            {
                references.Add(new SelfReference(child, "ref"));
            }

            FindDescendants(child, name, effectiveNamespace, references);
        }
    }

    private static bool Refers(SchemaSyntaxNode node, string attribute, string name, string effectiveNamespace)
    {
        var value = node.FindAttribute(attribute);
        if (value is null)
        {
            return false;
        }

        var colon = value.AsSpan().IndexOf(':');
        var local = colon < 0 ? value : value.Substring(colon + 1);
        if (!string.Equals(local, name, StringComparison.Ordinal))
        {
            return false;
        }

        var prefix = colon < 0 ? string.Empty : value.Substring(0, colon);
        var declared = node.TryResolvePrefix(prefix, out var namespaceUri);
        return (declared || colon < 0)
            && string.Equals(declared ? namespaceUri : string.Empty, effectiveNamespace, StringComparison.Ordinal);
    }

    // Redefine: the rewrite.

    private void ApplyRedefinitions()
    {
        if (_redefinitions.Count == 0)
        {
            return;
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in _units)
        {
            foreach (var component in unit.Components)
            {
                var name = component.FindAttribute("name");
                if (name is not null)
                {
                    _ = used.Add(Key(name, unit.EffectiveNamespace));
                }
            }
        }

        foreach (var redefinition in _redefinitions)
        {
            var original = FindOriginal(redefinition.Target, redefinition, []);
            if (original is null)
            {
                _reporter.Report(
                    SrcExpredef,
                    redefinition.Node.Location,
                    "The redefined schema document has no "
                        + redefinition.Node.Name.LocalName
                        + " named '"
                        + redefinition.Name
                        + "'."
                );
                _ = redefinition.Unit.Components.Remove(redefinition.Node);
                continue;
            }

            var counter = 1;
            string fresh;
            do
            {
                fresh = redefinition.Name + ".redefined" + counter++;
            } while (!used.Add(Key(fresh, redefinition.Unit.EffectiveNamespace)));

            original.SetAttribute("name", fresh);
            foreach (var reference in redefinition.References)
            {
                var value = reference.Node.FindAttribute(reference.Attribute)!;
                var colon = value.AsSpan().IndexOf(':');
                reference.Node.SetAttribute(reference.Attribute, colon < 0 ? fresh : value.Remove(colon + 1) + fresh);
            }
        }
    }

    private static SchemaSyntaxNode? FindOriginal(
        SchemaUnit unit,
        Redefinition redefinition,
        HashSet<SchemaUnit> visited
    )
    {
        if (!visited.Add(unit))
        {
            return null;
        }

        foreach (var component in unit.Components)
        {
            if (
                !ReferenceEquals(component, redefinition.Node)
                && string.Equals(component.Name.LocalName, redefinition.Node.Name.LocalName, StringComparison.Ordinal)
                && string.Equals(component.FindAttribute("name"), redefinition.Name, StringComparison.Ordinal)
            )
            {
                return component;
            }
        }

        foreach (var included in unit.Included)
        {
            var found = FindOriginal(included, redefinition, visited);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private sealed class SelfReference(SchemaSyntaxNode node, string attribute)
    {
        internal SchemaSyntaxNode Node { get; } = node;

        internal string Attribute { get; } = attribute;
    }

    private sealed class Redefinition(
        SchemaUnit unit,
        SchemaSyntaxNode node,
        string name,
        SchemaUnit target,
        List<SelfReference> references
    )
    {
        internal SchemaUnit Unit { get; } = unit;

        internal SchemaSyntaxNode Node { get; } = node;

        internal string Name { get; } = name;

        internal SchemaUnit Target { get; } = target;

        internal List<SelfReference> References { get; } = references;
    }
}
