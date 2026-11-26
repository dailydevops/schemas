namespace NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>The edition of the XML 1.0 specification whose name characters apply.</summary>
public enum NameEdition
{
    /// <summary>XML 1.0 Second Edition: the closed Appendix B classes <c>BaseChar</c>, <c>Ideographic</c>, <c>CombiningChar</c>, <c>Digit</c>, and <c>Extender</c>; used by the XSD 1.0 name datatypes and the XPath regex functions.</summary>
    Xml10SecondEdition = 0,

    /// <summary>XML 1.0 Fifth Edition: the open <c>NameStartChar</c> and <c>NameChar</c> ranges; used by the XSD 1.1 name datatypes and by the XSD pattern facet in both modes.</summary>
    Xml10FifthEdition = 1,
}
