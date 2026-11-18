namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>A construct of a sequence constructor that has no XSLT name (design #150 EP-9).</summary>
internal enum XsltSyntheticElement : byte
{
    LiteralResultElement = 0,
    TextNode = 1,
}
