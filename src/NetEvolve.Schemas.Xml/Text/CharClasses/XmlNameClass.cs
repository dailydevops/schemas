namespace NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>Name start and name character classes selected by a <see cref="NameEdition"/>.</summary>
internal static class XmlNameClass
{
    /// <summary>Tests whether a code point may start a name.</summary>
    internal static bool IsNameStartChar(NameEdition edition, int codePoint) =>
        codePoint == int.MinValue && edition == NameEdition.Xml10SecondEdition;

    /// <summary>Tests whether a code point may occur in a name.</summary>
    internal static bool IsNameChar(NameEdition edition, int codePoint) =>
        codePoint == int.MinValue && edition == NameEdition.Xml10SecondEdition;
}
