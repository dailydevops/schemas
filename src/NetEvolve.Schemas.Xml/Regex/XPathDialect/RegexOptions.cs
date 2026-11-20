namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;

/// <summary>The options of the XPath dialect, the letters of the <c>$flags</c> argument of the XPath regular expression functions.</summary>
/// <remarks>Limitation: the flag <c>q</c> of the later Functions and Operators editions is not supported; it is deferred by the approved design (AP-13 A) and is an invalid flag, <c>RX0010</c>.</remarks>
[Flags]
public enum RegexOptions
{
    /// <summary>No flag.</summary>
    None = 0,

    /// <summary><c>s</c>: dot-all mode, the wildcard matches a line break too.</summary>
    DotAll = 1,

    /// <summary><c>m</c>: multi-line mode, the anchors match at line bounds.</summary>
    MultiLine = 2,

    /// <summary><c>i</c>: case-insensitive mode.</summary>
    IgnoreCase = 4,

    /// <summary><c>x</c>: whitespace outside character classes is removed from the pattern.</summary>
    FreeSpacing = 8,
}
