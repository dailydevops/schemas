namespace NetEvolve.Schemas.Xml.Datatypes.Binary;

/// <summary>The two binary lexical spaces over the same octet value space.</summary>
public enum BinaryKind
{
    /// <summary><c>hexBinary</c> (XSD 1.1 Part 2, section 3.3.15).</summary>
    HexBinary = 0,

    /// <summary><c>base64Binary</c> (XSD 1.1 Part 2, section 3.3.16).</summary>
    Base64Binary = 1,
}
