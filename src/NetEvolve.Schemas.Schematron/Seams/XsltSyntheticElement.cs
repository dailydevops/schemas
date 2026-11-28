namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>A construct of a sequence constructor that has no XSLT name (design #150 EP-9).</summary>
internal enum XsltSyntheticElement : byte
{
    LiteralResultElement = 0,
    TextNode = 1,

    /// <summary>An instruction element that XSLT 2.0 and 3.0 do not define, with an <c>xsl:fallback</c> child (XSLT 2.0 section 18.2).</summary>
    UnknownInstruction = 2,
}
