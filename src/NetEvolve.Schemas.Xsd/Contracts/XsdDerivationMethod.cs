namespace NetEvolve.Schemas.Xsd.Contracts;

using System;

/// <summary>The methods of type derivation and the blocking sets built from them.</summary>
[Flags]
internal enum XsdDerivationMethod
{
    None = 0,
    Extension = 1,
    Restriction = 2,
    List = 4,
    Union = 8,
    Substitution = 16,
}
