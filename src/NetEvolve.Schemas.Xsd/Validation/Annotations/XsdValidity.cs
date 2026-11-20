namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

/// <summary>The validity of an element or attribute after assessment (Structures, validity property of the information set contributions).</summary>
internal enum XsdValidity
{
    /// <summary>The node was not assessed, so nothing is known about it.</summary>
    NotKnown = 0,

    /// <summary>The node was assessed and is not valid.</summary>
    Invalid = 1,

    /// <summary>The node was assessed and is valid.</summary>
    Valid = 2,
}
