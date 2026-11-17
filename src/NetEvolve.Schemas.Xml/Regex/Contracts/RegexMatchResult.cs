namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The outcome of one match call.</summary>
public enum RegexMatchResult
{
    /// <summary>The input does not match.</summary>
    NoMatch = 0,

    /// <summary>The input matches.</summary>
    Match = 1,

    /// <summary>A step ceiling was reached before the answer was known; this is neither a match nor a mismatch, and not an exception.</summary>
    Aborted = 2,
}
