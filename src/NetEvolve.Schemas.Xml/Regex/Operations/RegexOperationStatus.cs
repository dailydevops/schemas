namespace NetEvolve.Schemas.Xml.Regex.Operations;

/// <summary>The outcome of a replace or tokenize operation.</summary>
/// <remarks>
/// The numbers <c>12</c> and <c>13</c> continue the <c>RXnnnn</c> numbering of <see cref="Contracts.RegexError"/>: they are
/// the stable codes <c>RX0012</c> and <c>RX0013</c>, which the XPath functions map to <c>FORX0003</c> and <c>FORX0004</c>.
/// A step ceiling is not an error code, as in <see cref="Contracts.RegexMatchResult.Aborted"/>.
/// </remarks>
public enum RegexOperationStatus
{
    /// <summary>The operation completed.</summary>
    Success = 0,

    /// <summary>A step ceiling was reached before the result was known; no result is available.</summary>
    Aborted = 1,

    /// <summary>The destination is shorter than the result; the length of the result is reported.</summary>
    DestinationTooSmall = 2,

    /// <summary><c>RX0012</c>: the pattern matches the zero-length string, which replace and tokenize do not allow.</summary>
    ZeroLengthMatch = 12,

    /// <summary><c>RX0013</c>: the replacement string has a lone <c>$</c> or a lone backslash.</summary>
    InvalidReplacement = 13,
}
