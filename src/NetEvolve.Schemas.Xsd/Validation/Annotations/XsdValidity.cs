#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the annotated result and its annotations (design #146 DP-1, DP-18)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The validity of an element or attribute after assessment (Structures, validity property of the information set contributions).</summary>
public enum XsdValidity
{
    /// <summary>The node was not assessed, so nothing is known about it.</summary>
    NotKnown = 0,

    /// <summary>The node was assessed and is not valid.</summary>
    Invalid = 1,

    /// <summary>The node was assessed and is valid.</summary>
    Valid = 2,
}
