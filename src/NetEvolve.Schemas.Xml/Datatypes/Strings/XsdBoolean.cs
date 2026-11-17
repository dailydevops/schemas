namespace NetEvolve.Schemas.Xml.Datatypes.Strings;

/// <summary>The canonical lexical form of xs:boolean (XSD 1.1 Part 2, section 3.3.2).</summary>
public static class XsdBoolean
{
    /// <summary>Gets the canonical form of a value.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> or <see langword="false"/> as text.</returns>
    public static string ToCanonical(bool value) => value ? "true" : "false";
}
