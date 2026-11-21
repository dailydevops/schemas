namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The structured content of one referenced property: an owned snapshot, valid after the evaluation that built it, so the report
/// writer can keep it until the end of the pattern. The attributes that <c>xsl:copy-of</c> copied belong to the element that holds the
/// content, so they are written before its start tag ends; the children follow.
/// </summary>
internal interface IFindingContent
{
    /// <summary>Writes the namespace declarations and the attributes of the content into the start tag of the holder.</summary>
    /// <param name="sink">The events of the holder; its start tag is still open.</param>
    void WriteAttributes(IXmlSink sink);

    /// <summary>Writes the children of the content into the holder, after its start tag ended.</summary>
    /// <param name="sink">The events of the holder.</param>
    void WriteChildren(IXmlSink sink);
}
