namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The seam for general entity references (design #108, AP20). The parser calls it exactly where it would otherwise report
/// <c>XML0008</c>; the implementation decides the constraints of sections 4.1 and 4.4, charges the expansion budgets, and
/// hands back the replacement text that the parser reads as a frame over the outer input.
/// </summary>
/// <remarks>
/// An internal entity is handed out as its replacement text. An external parsed entity in content is handed out as an
/// <see cref="IXmlEntityText"/> that the parser reads chunk by chunk as a frame and disposes (XML-62); without a loader
/// behind the stack the reference is <c>XML2002</c>. A reference in an attribute value to an external entity is
/// <c>XML0022</c>, and to an unparsed entity <c>XML0021</c>, in both places.
/// </remarks>
internal interface IXmlEntityInputStack
{
    /// <summary>Gets the base URI of the entity whose text is being read: the document base URI outside of an external entity, otherwise the base URI of the innermost one.</summary>
    string? BaseUri { get; }

    /// <summary>Called for a reference to an entity that is not predefined.</summary>
    /// <param name="name">The entity name without the ampersand and the semicolon; valid only during the call.</param>
    /// <param name="inAttributeValue">Whether the reference stands in an attribute value (section 3.3.3) and not in content.</param>
    /// <param name="standalone">Whether the XML declaration says <c>standalone="yes"</c>.</param>
    /// <param name="replacement">
    /// The replacement text to read when the reference is expanded, <see langword="null"/> when it is skipped or the entity is
    /// external. A non-null text is a frame that the caller must close with <see cref="Leave"/>. Its line ends are already
    /// normalized (CR LF and a lone CR written in the entity value became LF, section 2.11), so the parser reads every carriage
    /// return in it as a character of its own, one that came from a character reference.
    /// </param>
    /// <param name="external">
    /// The text of an external parsed entity, <see langword="null"/> otherwise; only for a reference in content. The caller reads
    /// it as a frame, disposes it, and closes the frame with <see cref="Leave"/>. Its line ends are already normalized.
    /// </param>
    /// <returns>The error code to report, or <see langword="null"/> when the reference is accepted (expanded or skipped).</returns>
    XmlErrorCode? Enter(
        ReadOnlySpan<char> name,
        bool inAttributeValue,
        bool standalone,
        out string? replacement,
        out IXmlEntityText? external
    );

    /// <summary>Closes the frame of the last accepted expansion and gives its depth back.</summary>
    void Leave();
}
