namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The diagnostic channel of <c>xsl:message</c> (XSLT 2.0 section 17): the message goes there and evaluation continues. The
/// destination is implementation-defined; the owner of the run decides what the channel does with the text.
/// </summary>
internal interface IXsltMessageSink
{
    /// <summary>Receives a message.</summary>
    /// <param name="text">The string value of the message.</param>
    /// <param name="location">The location of the <c>xsl:message</c> element.</param>
    void Message(string text, DocumentLocation location);
}
