namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>A component that has no name.</summary>
internal sealed class UnnamedComponent : IXsdComponent
{
    public XsdComponentKind Kind => XsdComponentKind.ElementDeclaration;

    public XPathQName Name => default;

    public DocumentLocation Location => default;
}
