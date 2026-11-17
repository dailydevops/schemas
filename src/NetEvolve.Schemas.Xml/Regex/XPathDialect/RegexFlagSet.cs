namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;

/// <summary>The flags of the XPath regex dialect, as the letters of the flags argument of <c>fn:matches</c>.</summary>
/// <remarks>Limitation: only <c>s</c>, <c>m</c>, <c>i</c>, <c>x</c>, and <c>q</c> exist; the <c>i</c> flag uses the simple case folding of the embedded Unicode tables, so a full folding such as U+00DF to <c>ss</c> is not applied.</remarks>
[Flags]
public enum RegexFlagSet
{
    /// <summary>No flag.</summary>
    None = 0,

    /// <summary><c>s</c>: <c>.</c> also matches a line feed and a carriage return.</summary>
    DotAll = 1,

    /// <summary><c>m</c>: <c>^</c> and <c>$</c> match at the bounds of every line, not only of the input.</summary>
    MultiLine = 2,

    /// <summary><c>i</c>: characters match when they have the same simple case folding.</summary>
    IgnoreCase = 4,

    /// <summary><c>x</c>: white space in the pattern outside a character class is removed.</summary>
    Extended = 8,

    /// <summary><c>q</c>: the pattern is literal text.</summary>
    Literal = 16,
}
