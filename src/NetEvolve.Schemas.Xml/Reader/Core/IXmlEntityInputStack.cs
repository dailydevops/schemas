namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The seam for general entity references (design #108, AP20). The parser calls it exactly where it would otherwise report
/// <c>XML0008</c>; the implementation decides the constraints of sections 4.1 and 4.4, charges the expansion budgets, and
/// hands back the replacement text that the parser reads as a frame over the outer input.
/// </summary>
/// <remarks>
/// Limitation: only internal general entities are expanded; a reference to an external parsed entity in content is
/// <c>XML2002</c> until the external entity packages (XML-61, XML-62) plug a resolver in.
/// </remarks>
internal interface IXmlEntityInputStack
{
    /// <summary>Called for a reference to an entity that is not predefined.</summary>
    /// <param name="name">The entity name without the ampersand and the semicolon; valid only during the call.</param>
    /// <param name="inAttributeValue">Whether the reference stands in an attribute value (section 3.3.3) and not in content.</param>
    /// <param name="standalone">Whether the XML declaration says <c>standalone="yes"</c>.</param>
    /// <param name="replacement">
    /// The replacement text to read when the reference is expanded, <see langword="null"/> when it is skipped. A
    /// non-null text is a frame that the caller must close with <see cref="Leave"/>. Its line ends are already normalized
    /// (CR LF and a lone CR written in the entity value became LF, section 2.11), so the parser reads every carriage return
    /// in it as a character of its own, one that came from a character reference.
    /// </param>
    /// <returns>The error code to report, or <see langword="null"/> when the reference is accepted (expanded or skipped).</returns>
    XmlErrorCode? Enter(ReadOnlySpan<char> name, bool inAttributeValue, bool standalone, out string? replacement);

    /// <summary>Closes the frame of the last accepted expansion and gives its depth back.</summary>
    void Leave();
}
