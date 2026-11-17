namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A wildcard over an explicit set of namespaces that logs every call.</summary>
internal sealed class FakeWildcard : IWildcard
{
    private readonly HashSet<string> _namespaces;
    private readonly List<string> _log;

    public FakeWildcard(List<string> log, XsdProcessContents processContents, params string[] namespaces)
    {
        _log = log;
        ProcessContents = processContents;
        _namespaces = new HashSet<string>(namespaces, StringComparer.Ordinal);
    }

    public XsdComponentKind Kind => XsdComponentKind.Wildcard;

    public XPathQName Name => default;

    public DocumentLocation Location => default;

    public XsdProcessContents ProcessContents { get; }

    public bool Allows(XPathQName name)
    {
        _log.Add($"Allows:{name.NamespaceName}");
        return _namespaces.Contains(name.NamespaceName);
    }

    public bool IsSubsetOf(IWildcard other)
    {
        _log.Add("IsSubsetOf");
        foreach (var ns in _namespaces)
        {
            if (!other.Allows(new XPathQName(ns, "x")))
            {
                return false;
            }
        }

        return true;
    }

    public IWildcard? Union(IWildcard other)
    {
        _log.Add("Union");
        var result = new FakeWildcard(_log, ProcessContents);
        result._namespaces.UnionWith(_namespaces);
        result._namespaces.UnionWith(((FakeWildcard)other)._namespaces);
        return result;
    }

    public IWildcard? Intersect(IWildcard other)
    {
        _log.Add("Intersect");
        var intersection = new HashSet<string>(_namespaces, StringComparer.Ordinal);
        intersection.IntersectWith(((FakeWildcard)other)._namespaces);

        // Nothing left means no expressible wildcard: the fake mirrors the null result of the contract.
        return intersection.Count == 0 ? null : new FakeWildcard(_log, ProcessContents, [.. intersection]);
    }
}
