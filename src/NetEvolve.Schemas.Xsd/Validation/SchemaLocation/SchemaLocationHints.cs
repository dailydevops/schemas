namespace NetEvolve.Schemas.Xsd.Validation.SchemaLocation;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The schema location hints of one run (Structures 2.6.3 and 4.3.2, design #146 DP-22): reads <c>xsi:schemaLocation</c> and
/// <c>xsi:noNamespaceSchemaLocation</c> where the validator meets them, loads the named documents through the resolver, compiles them,
/// and extends the model of the run with them. The model of the validator is never changed, so the effect ends with the run.
/// </summary>
/// <remarks>
/// <para>
/// A hint is advisory: it is read where the attributes of its element are complete, and a hint for a namespace that an element or
/// attribute of the document already used is an error and loads nothing. The element that carries the hint does not count as a use
/// before its hints are read. A hint for a namespace that the schema already defines, or that an earlier hint of the run loaded, is ignored without a request. Every request counts
/// against <see cref="ValidationLimits.MaxExternalResources"/>, those that the hinted schema makes for its own includes and imports
/// too; the size of a document against <see cref="ValidationLimits.MaxResourceSize"/>.
/// </para>
/// <para>
/// Limitations: a hinted schema is compiled on its own, so it cannot refer to components of the configured schema (#959), and a component that
/// it shares by name with the configured schema is an error.
/// </para>
/// </remarks>
internal sealed class SchemaLocationHints
{
    private static readonly XsdRule SchemaReference = XsdRuleLookup.Find(XsdRuleCodes.SchemaReference);

    private readonly XsdVersion _version;
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;
    private readonly XsdIssueReporter _reporter;
    private readonly string? _documentUri;
    private readonly CancellationToken _cancellationToken;
    private readonly HashSet<string> _used = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hinted = new(StringComparer.Ordinal);
    private Dictionary<IXsdComponent, IReadOnlyList<IdentityConstraintDefinition>>? _constraints;
    private int _requests;
    private bool _limitReported;

    /// <summary>Initializes a new instance of the <see cref="SchemaLocationHints"/> class.</summary>
    /// <param name="version">The XSD version that a hinted schema is compiled for.</param>
    /// <param name="resolver">The resolver that every location goes through.</param>
    /// <param name="limits">The limits of the run.</param>
    /// <param name="reporter">The reporter for the issues of the hints.</param>
    /// <param name="documentUri">The system identifier of the instance, the base of relative locations; <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">The token that is passed to the resolver.</param>
    internal SchemaLocationHints(
        XsdVersion version,
        IResourceResolver resolver,
        ValidationLimits limits,
        XsdIssueReporter reporter,
        string? documentUri,
        CancellationToken cancellationToken
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(reporter);

        _version = version;
        _resolver = resolver;
        _limits = limits;
        _reporter = reporter;
        _documentUri = documentUri;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Gets a value indicating whether a loaded schema names an ID, IDREF, IDREFS, or NOTATION type.</summary>
    internal bool NeedsIds { get; private set; }

    /// <summary>Gets a value indicating whether a loaded schema names an ENTITY or ENTITIES type.</summary>
    internal bool NeedsEntities { get; private set; }

    /// <summary>Gets a value indicating whether a loaded schema declares an identity constraint.</summary>
    internal bool HasConstraints => _constraints is not null;

    /// <summary>Gives the identity constraints that the loaded schemas of the run declare for an element declaration.</summary>
    /// <param name="declaration">The element declaration.</param>
    /// <returns>The constraints, <see langword="null"/> for none.</returns>
    internal IReadOnlyList<IdentityConstraintDefinition>? ConstraintsOf(IXsdComponent declaration) =>
        _constraints is not null && _constraints.TryGetValue(declaration, out var constraints) ? constraints : null;

    /// <summary>Records that an element or attribute of a namespace is assessed, so a later hint for the namespace is an error.</summary>
    /// <param name="namespaceName">The namespace name, empty for none.</param>
    internal void NoteUse(string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);

        _ = _used.Add(namespaceName);
    }

    /// <summary>Applies the value of <c>xsi:schemaLocation</c>: pairs of a namespace and a location.</summary>
    /// <param name="model">The model of the run so far.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="location">The location of the element that carries the attribute.</param>
    /// <returns>The model with the loaded schemas, <paramref name="model"/> itself when nothing was loaded.</returns>
    internal ValidationModel ApplySchemaLocation(
        ValidationModel model,
        ReadOnlySpan<char> value,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(model);

        var count = CountTokens(value);
        if (count % 2 != 0)
        {
            // The attribute is a hint (Structures 4.3.2): a value that is not a list of pairs is skipped, so the
            // schema set alone judges the instance, as System.Xml.Schema does.
            return model;
        }

        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        while (cursor.TryNext(value, out var namespaceRange))
        {
            _ = cursor.TryNext(value, out var locationRange);
            model = Apply(model, Text(value, namespaceRange), Text(value, locationRange), location);
        }

        return model;
    }

    /// <summary>Applies the value of <c>xsi:noNamespaceSchemaLocation</c>: one location of a schema without a target namespace.</summary>
    /// <param name="model">The model of the run so far.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="location">The location of the element that carries the attribute.</param>
    /// <returns>The model with the loaded schema, <paramref name="model"/> itself when nothing was loaded.</returns>
    internal ValidationModel ApplyNoNamespaceSchemaLocation(
        ValidationModel model,
        ReadOnlySpan<char> value,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(model);

        var count = CountTokens(value);
        if (count != 1)
        {
            Report(
                location,
                "The value of xsi:noNamespaceSchemaLocation has "
                    + count
                    + " tokens, but it must be exactly one location."
            );
            return model;
        }

        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        _ = cursor.TryNext(value, out var range);
        return Apply(model, string.Empty, Text(value, range), location);
    }

    // The value is a list of anyURI items (Structures 2.6.3), separated by any XML white space.
    private static int CountTokens(ReadOnlySpan<char> value)
    {
        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        var count = 0;
        while (cursor.TryNext(value, out _))
        {
            count++;
        }

        return count;
    }

    private static string Text(ReadOnlySpan<char> value, TextRange range) =>
        value.Slice(range.Offset, range.Length).ToString();

    private ValidationModel Apply(ValidationModel model, string namespaceName, string reference, DocumentLocation where)
    {
        if (_used.Contains(namespaceName))
        {
            Report(
                where,
                "The schema location hint for the namespace "
                    + Describe(namespaceName)
                    + " comes after the first use of the namespace in the document."
            );
            return model;
        }

        // The schema already describes the namespace, or an earlier hint loaded it: the hint is ignored (Structures 4.3.2 lets a
        // processor use the schema that it has), so an instance may repeat the hint for the schema that the builder was given.
        if (!_hinted.Add(namespaceName) || model.Schema.DefinesNamespace(namespaceName))
        {
            return model;
        }

        if (_requests >= _limits.MaxExternalResources)
        {
            ReportLimit(where);
            return model;
        }

        var resolution = UriReference.Resolve(reference, _documentUri);
        if (resolution.Uri is null && resolution.Code == CoreDiagnosticCodes.ResourceReferenceInvalid)
        {
            Core(resolution.Code, where, "The schema location '" + reference + "' is not a valid URI reference.");
            return model;
        }

        // The loader counts what it loads; a fresh one per hint, because the documents that a hinted schema loads count in _requests too.
        var loader = new ResourceLoader(
            _resolver,
            _limits.WithMaxExternalResources(_limits.MaxExternalResources - _requests)
        )
        {
            PassMissingBaseToResolver = true,
        };
        var result = loader.Load(
            ResourceKind.SchemaLocationHint,
            reference,
            _documentUri,
            _cancellationToken,
            namespaceUri: namespaceName.Length == 0 ? null : namespaceName
        );
        _requests += loader.RequestCount;
        if (result.Status != ResourceLoadStatus.Loaded)
        {
            Core(result.Code, where, result.Message);
            return model;
        }

        return Load(model, namespaceName, result.Content.Span, result.SystemId ?? reference, where);
    }

    // Compiles the document on its own and, when it is the one that the hint promised and shares no component with the
    // model, extends the model with it.
    private ValidationModel Load(
        ValidationModel model,
        string namespaceName,
        ReadOnlySpan<byte> content,
        string systemId,
        DocumentLocation where
    )
    {
        var remaining = _limits.MaxExternalResources - _requests;
        var budget = new BudgetResolver(_resolver, remaining);
        var run = new CompilationRun(_version, budget, _limits.WithMaxExternalResources(Math.Max(1, remaining)));
        var document = run.Read(content, systemId);
        if (document.Root is { } root)
        {
            var found = root.FindAttribute("targetNamespace") ?? string.Empty;
            if (!string.Equals(found, namespaceName, StringComparison.Ordinal))
            {
                Report(
                    where,
                    "The target namespace "
                        + Describe(found)
                        + " of the schema document '"
                        + systemId
                        + "' is not "
                        + Describe(namespaceName)
                        + ", the namespace of the hint."
                );
                return model;
            }
        }

        var compilation = run.Execute([document], _cancellationToken);
        _requests += budget.Forwarded;
        if (!compilation.IsValid)
        {
            Forward(compilation.Result.Issues);
            return model;
        }

        if (ReportDuplicates(model, compilation.Components, where))
        {
            return model;
        }

        NeedsIds |= compilation.NeedsIdListener;
        NeedsEntities |= compilation.NeedsEntityListener;
        if (compilation.IdentityConstraints.Count > 0)
        {
            _constraints ??= [];
            foreach (var pair in compilation.IdentityConstraints)
            {
                _constraints[pair.Key] = pair.Value;
            }
        }

        return model.Extend(compilation.Set!.WithParent(model.Schema), compilation.Components, _reporter);
    }

    // A component that the model already has under the same name is an error; the built-in types are the same objects everywhere.
    private bool ReportDuplicates(
        ValidationModel model,
        IReadOnlyList<IXsdComponent> components,
        DocumentLocation where
    )
    {
        var found = false;
        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            if (
                component.Name.LocalName.Length > 0
                && model.Schema.TryGet(component.Kind, component.Name, out var existing)
                && !ReferenceEquals(existing, component)
            )
            {
                found = true;
                _reporter.Report(
                    XsdRuleLookup.SchPropsCorrect,
                    where,
                    "The schema document of the hint declares "
                        + component.Kind
                        + " "
                        + StreamingValidator.Display(component.Name)
                        + ", which the schema already has.",
                    "sch-props-correct.2"
                );
            }
        }

        return found;
    }

    // The issues of a hinted schema that did not compile; a request that the budget refused is the limit, not a refusal.
    private void Forward(IReadOnlyList<ValidationIssue> issues)
    {
        for (var i = 0; i < issues.Count; i++)
        {
            var issue = issues[i];
            _reporter.Add(
                issue.Code == CoreDiagnosticCodes.ResourceDenied
                && string.Equals(issue.Message, BudgetResolver.LimitMessage, StringComparison.Ordinal)
                    ? new ValidationIssue(
                        CoreDiagnosticCodes.ExternalResourceLimitExceeded,
                        issue.Severity,
                        "More than " + _limits.MaxExternalResources + " external resources are used.",
                        issue.Location,
                        issue.Constraint
                    )
                    : issue
            );
        }
    }

    private void ReportLimit(DocumentLocation where)
    {
        if (_limitReported)
        {
            return;
        }

        _limitReported = true;
        Core(
            CoreDiagnosticCodes.ExternalResourceLimitExceeded,
            where,
            "More than " + _limits.MaxExternalResources + " external resources are used."
        );
    }

    private void Report(DocumentLocation where, string message) => _reporter.Report(SchemaReference, where, message);

    private void Core(DiagnosticCode code, DocumentLocation where, string message) =>
        _reporter.Add(new ValidationIssue(code, IssueSeverity.Error, message, where, SchemaReference.Id));

    private static string Describe(string namespaceName) =>
        namespaceName.Length == 0 ? "(none)" : "'" + namespaceName + "'";
}
