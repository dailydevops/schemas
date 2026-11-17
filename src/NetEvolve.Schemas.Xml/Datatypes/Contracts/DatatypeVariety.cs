namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The variety of a simple type (XSD 1.1 Part 2, section 4.1.1).</summary>
public enum DatatypeVariety
{
    /// <summary>A type whose values are indivisible.</summary>
    Atomic = 0,

    /// <summary>A type whose values are finite sequences of atomic values.</summary>
    List = 1,

    /// <summary>A type whose value space is the union of the value spaces of its member types.</summary>
    Union = 2,
}
