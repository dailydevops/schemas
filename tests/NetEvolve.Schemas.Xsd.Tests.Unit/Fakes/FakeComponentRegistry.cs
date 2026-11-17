namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A registry that keeps components in a dictionary and logs every call.</summary>
internal sealed class FakeComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<(XsdComponentKind, XPathQName), IXsdComponent> _components = [];
    private readonly List<string> _log;

    public FakeComponentRegistry(List<string> log) => _log = log;

    public int Count => _components.Count;

    public void Add(IXsdComponent component)
    {
        _log.Add($"Add:{component.Kind}:{component.Name.LocalName}");
        _components[(component.Kind, component.Name)] = component;
    }

    public bool TryGet(XsdComponentKind kind, XPathQName name, out IXsdComponent? component)
    {
        _log.Add($"TryGet:{kind}:{name.LocalName}");
        return _components.TryGetValue((kind, name), out component);
    }
}
