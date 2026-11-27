namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The attribute uses and the attribute wildcard of a complex type or of an attribute group, as one side of a restriction check.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AttributeSide
{
    internal AttributeSide(IReadOnlyList<AttributeUse> uses, IWildcard? wildcard)
    {
        Uses = uses;
        Wildcard = wildcard;
    }

    /// <summary>Gets the attribute uses.</summary>
    internal IReadOnlyList<AttributeUse> Uses { get; }

    /// <summary>Gets the attribute wildcard, <see langword="null"/> for none.</summary>
    internal IWildcard? Wildcard { get; }
}
