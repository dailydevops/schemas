namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;

/// <summary>A located attribute of a schema document element.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XsdSyntaxAttribute
{
    internal XsdSyntaxAttribute(XPathQName name, string value, DocumentLocation location)
    {
        Name = name;
        Value = value;
        Location = location;
    }

    /// <summary>Gets the expanded name.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the normalized value.</summary>
    public string Value { get; }

    /// <summary>Gets the location of the attribute.</summary>
    public DocumentLocation Location { get; }
}
