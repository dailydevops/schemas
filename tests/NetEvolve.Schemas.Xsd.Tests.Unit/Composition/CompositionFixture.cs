namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Builds schema documents and loads them through the loader under test.</summary>
internal static class CompositionFixture
{
    public const string RootUri = "file:///root.xsd";

    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public static string Schema(string body, string? targetNamespace = null) =>
        "<xs:schema xmlns:xs=\""
        + Xs
        + "\""
        + (targetNamespace is null ? string.Empty : " targetNamespace=\"" + targetNamespace + "\"")
        + (targetNamespace is null ? string.Empty : " xmlns:t=\"" + targetNamespace + "\"")
        + ">\n"
        + body
        + "\n</xs:schema>";

    public static (SchemaDocumentSet Set, IReadOnlyList<ValidationIssue> Issues) Load(
        string root,
        IResourceResolver resolver,
        ValidationLimits? limits = null
    )
    {
        limits ??= ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var document = SchemaDocumentParser.Parse(root.AsSpan(), RootUri, new XsdIssueReporter(collector));
        var set = new SchemaDocumentLoader(resolver, limits, collector).Load(document, CancellationToken.None);
        return (set, collector.ToResult().Issues);
    }

    /// <summary>Loads a document that has no base URI.</summary>
    public static (SchemaDocumentSet Set, IReadOnlyList<ValidationIssue> Issues) LoadWithoutBase(
        string root,
        IResourceResolver resolver
    )
    {
        var limits = ValidationLimits.Default;
        var collector = new ValidationIssueCollector(limits);
        var document = SchemaDocumentParser.Parse(root.AsSpan(), null, new XsdIssueReporter(collector));
        var set = new SchemaDocumentLoader(resolver, limits, collector).Load(document, CancellationToken.None);
        return (set, collector.ToResult().Issues);
    }

    public static DiagnosticCode CodeOf(string ruleId)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
            {
                return rule.Code;
            }
        }

        throw new InvalidOperationException("No rule " + ruleId);
    }

    /// <summary>Describes the components of a unit as <c>kind:name</c> pairs in order.</summary>
    public static string Names(SchemaUnit unit)
    {
        var builder = new StringBuilder();
        foreach (var component in unit.Components)
        {
            builder
                .Append(builder.Length == 0 ? string.Empty : ",")
                .Append(component.Name.LocalName)
                .Append(':')
                .Append(component.FindAttribute("name"));
        }

        return builder.ToString();
    }
}
