namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The input of <see cref="ValidationListener.StartElement"/>, delivered once all attributes are known.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ElementStartInfo
{
    internal ElementStartInfo(
        int ordinal,
        XmlName name,
        DocumentLocation location,
        IXsdComponent? declaration,
        IXsdComponent? type,
        int depth
    )
    {
        Ordinal = ordinal;
        Name = name;
        Location = location;
        Declaration = declaration;
        Type = type;
        Depth = depth;
    }

    /// <summary>Gets the ordinal of the element start event in document order.</summary>
    public int Ordinal { get; }

    /// <summary>Gets the name handle.</summary>
    public XmlName Name { get; }

    /// <summary>Gets the location of the start tag.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets the element declaration, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Declaration { get; }

    /// <summary>Gets the type chosen for the element, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Type { get; }

    /// <summary>Gets the element nesting depth, the root being 1.</summary>
    public int Depth { get; }
}
