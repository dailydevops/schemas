namespace NetEvolve.Schemas.Xsd.Components;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;

/// <summary>A by-name reference from a component to another component.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XsdReference
{
    internal XsdReference(XsdSymbolSpace space, XPathQName name, DocumentLocation location, bool isStructural)
    {
        Space = space;
        Name = name;
        Location = location;
        IsStructural = isStructural;
    }

    /// <summary>Gets the symbol space the name is looked up in.</summary>
    public XsdSymbolSpace Space { get; }

    /// <summary>Gets the referenced expanded name.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the location of the reference in the schema document.</summary>
    public DocumentLocation Location { get; }

    /// <summary>
    /// Gets a value indicating whether the reference is part of the definition itself (base type, member type, group or
    /// attribute group reference). Only these edges can form a circular definition; a reference through an element
    /// declaration is not one.
    /// </summary>
    public bool IsStructural { get; }
}
