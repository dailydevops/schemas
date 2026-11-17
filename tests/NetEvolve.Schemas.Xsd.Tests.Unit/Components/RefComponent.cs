namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
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
