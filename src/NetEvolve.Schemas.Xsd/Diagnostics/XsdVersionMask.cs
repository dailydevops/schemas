#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

using System;

/// <summary>The set of XSD versions in which a rule applies.</summary>
[Flags]
public enum XsdVersionMask
{
    /// <summary>No version.</summary>
    None = 0,

    /// <summary>XML Schema 1.0.</summary>
    V10 = 1,

    /// <summary>XML Schema 1.1.</summary>
    V11 = 2,

    /// <summary>Both versions.</summary>
    All = V10 | V11,
}
