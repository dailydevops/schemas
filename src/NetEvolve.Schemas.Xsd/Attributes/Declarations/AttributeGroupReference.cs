namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;

/// <summary>A located reference to an attribute group by expanded name.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AttributeGroupReference
{
    internal AttributeGroupReference(XPathQName name, DocumentLocation location)
    {
        Name = name;
        Location = location;
    }

    /// <summary>Gets the expanded name of the referenced group.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the location of the reference.</summary>
    public DocumentLocation Location { get; }
}
