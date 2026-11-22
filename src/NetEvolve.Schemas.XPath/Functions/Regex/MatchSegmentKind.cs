namespace NetEvolve.Schemas.XPath.Functions.Regex;

/// <summary>The kind of a segment of an input that a pattern divides.</summary>
internal enum MatchSegmentKind
{
    /// <summary>A part of the input between two matches, before the first, or after the last; it is never empty.</summary>
    NonMatch = 0,

    /// <summary>A match of the pattern; it is empty only for a pattern that matches the zero-length string.</summary>
    Match = 1,
}
