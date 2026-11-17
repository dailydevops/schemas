namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The anchors of the XPath dialect.</summary>
public enum RegexAnchor
{
    /// <summary>The start of the input, or of a line with the <c>m</c> flag.</summary>
    Start = 0,

    /// <summary>The end of the input, or of a line with the <c>m</c> flag.</summary>
    End = 1,
}
