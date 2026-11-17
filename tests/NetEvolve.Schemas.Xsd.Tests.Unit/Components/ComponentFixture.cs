namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>A named component with references that records what the resolver binds.</summary>
internal sealed class RefComponent : IReferencingComponent
{
    private readonly XsdReference[] _references;

    public RefComponent(XsdComponentKind kind, string name, int line, params XsdReference[] references)
    {
        Kind = kind;
        Name = Fixture.Name(name);
        Location = new DocumentLocation("file:///a.xsd", line, 1);
        _references = references;
        Bound = new IXsdComponent?[references.Length];
    }

    public IXsdComponent?[] Bound { get; }

    public XsdComponentKind Kind { get; }

    public XPathQName Name { get; }

    public DocumentLocation Location { get; }

    public int ReferenceCount => _references.Length;

    public XsdReference GetReference(int index) => _references[index];

    public void Bind(int index, IXsdComponent target) => Bound[index] = target;
}

/// <summary>A component that has no name.</summary>
internal sealed class UnnamedComponent : IXsdComponent
{
    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    public XPathQName Name => default;

    public DocumentLocation Location => default;
}

internal static class Fixture
{
    public static XPathQName Name(string local) => new("urn:test", local);

    public static XsdReference Ref(XsdSymbolSpace space, string name, int line = 1, bool structural = true) =>
        new(space, Name(name), new DocumentLocation("file:///a.xsd", line, 2), structural);

    public static ComponentRegistry NewRegistry(out ValidationIssueCollector collector)
    {
        collector = new ValidationIssueCollector(ValidationLimits.Default);
        return new ComponentRegistry(new XsdIssueReporter(collector));
    }

    public static IReadOnlyList<ValidationIssue> Resolve(
        ComponentRegistry registry,
        ValidationIssueCollector collector,
        XsdVersion version = XsdVersion.V11
    )
    {
        new ComponentResolver(version).Resolve(registry, new XsdIssueReporter(collector));
        return collector.ToResult().Issues;
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
}
