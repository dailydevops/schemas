namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The seam for general entity references (design #108, AP20). The reader calls it exactly where it would otherwise report
/// <c>XML0008</c>; frames and expansion accounting are XML-59.
/// </summary>
/// <remarks>
/// Limitation: until XML-59 plugs in, the reader reports the returned code and, for a <see langword="null"/> return, still
/// reports <c>XML0008</c>, because no frame can be entered yet.
/// </remarks>
internal interface IXmlEntityInputStack
{
    /// <summary>Called for a reference to an entity that is not predefined.</summary>
    /// <param name="name">The entity name without the ampersand and the semicolon; valid only during the call.</param>
    /// <param name="referencePosition">The position of the ampersand in the outer input.</param>
    /// <returns>The error code to report, or <see langword="null"/> when the reference is accepted.</returns>
    XmlErrorCode? Enter(ReadOnlySpan<char> name, XmlPosition referencePosition);
}
