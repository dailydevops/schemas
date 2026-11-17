namespace NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The values of the <c>whiteSpace</c> facet (XSD 1.1 Part 2, section 4.3.6).</summary>
public enum WhitespaceMode
{
    /// <summary>No normalization is performed.</summary>
    Preserve = 0,

    /// <summary>Each tab, line feed, and carriage return is replaced with a space.</summary>
    Replace = 1,

    /// <summary>After replacing, runs of spaces collapse to one space and leading and trailing spaces are removed.</summary>
    Collapse = 2,
}
