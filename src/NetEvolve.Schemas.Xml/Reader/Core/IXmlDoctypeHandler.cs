namespace NetEvolve.Schemas.Xml.Reader.Core;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The seam for the document type declaration (design #108, AP12 and AP20). Without a handler the reader rejects the
/// declaration with <c>XML2001</c> at its opener and scans nothing of it.
/// </summary>
/// <remarks>
/// Limitation: the reader scans the declaration lexically only (name, identifiers, quoted literals, comments and processing
/// instructions of the internal subset) and does not validate public identifier characters; parsing the subset is XML-14.
/// </remarks>
internal interface IXmlDoctypeHandler
{
    /// <summary>Receives the declaration; the spans are valid only during the call.</summary>
    /// <param name="doctype">The parts of the declaration.</param>
    /// <returns><see langword="null"/> to accept, or the error code to report (for example <c>XML2002</c> for external access).</returns>
    XmlErrorCode? Handle(in XmlDoctypeInfo doctype);
}
