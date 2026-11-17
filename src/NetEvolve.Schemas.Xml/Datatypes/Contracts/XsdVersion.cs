namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The XSD version a descriptor follows; it is a parameter of every lookup, never global state.</summary>
public enum XsdVersion
{
    /// <summary>XML Schema 1.0, Part 2: Datatypes, Second Edition.</summary>
    V10 = 0,

    /// <summary>XML Schema 1.1, Part 2: Datatypes.</summary>
    V11 = 1,
}
