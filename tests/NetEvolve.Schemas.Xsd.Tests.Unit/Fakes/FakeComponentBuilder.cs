namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>A builder that names the component after the <c>name</c> attribute, registers it, and logs the call.</summary>
internal sealed class FakeComponentBuilder : IComponentBuilder
{
    private readonly List<string> _log;

    public FakeComponentBuilder(XsdComponentKind kind, List<string> log)
    {
        Kind = kind;
        _log = log;
    }

    public XsdComponentKind Kind { get; }

    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        _log.Add($"Build:{element.Name.LocalName}@{element.Location.Line}");

        var name = string.Empty;
        for (var i = 0; i < element.AttributeCount; i++)
        {
            var attribute = element.GetAttribute(i);
            if (string.Equals(attribute.Name.LocalName, "name", StringComparison.Ordinal))
            {
                name = attribute.Value;
            }
        }

        var component = new FakeComponent(Kind, name, element.Location.Line);
        registry.Add(component);
        return component;
    }
}
