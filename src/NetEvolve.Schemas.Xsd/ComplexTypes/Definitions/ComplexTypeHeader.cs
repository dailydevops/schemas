namespace NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The properties every complex type definition has, whatever its content: name, location, abstract, block, and final.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ComplexTypeHeader
{
    internal ComplexTypeHeader(
        XPathQName name,
        DocumentLocation location,
        bool isAbstract,
        XsdDerivationMethod block,
        XsdDerivationMethod final
    )
    {
        Name = name;
        Location = location;
        IsAbstract = isAbstract;
        Block = block;
        Final = final;
    }

    /// <summary>Gets the expanded name, <see langword="default"/> for an anonymous type.</summary>
    public XPathQName Name { get; }

    /// <summary>Gets the location of the declaring syntax.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets a value indicating whether the type is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the substitutions of derived types that are blocked; only extension and restriction apply.</summary>
    public XsdDerivationMethod Block { get; }

    /// <summary>Gets the derivations that are not allowed from this type; only extension and restriction apply.</summary>
    public XsdDerivationMethod Final { get; }
}
