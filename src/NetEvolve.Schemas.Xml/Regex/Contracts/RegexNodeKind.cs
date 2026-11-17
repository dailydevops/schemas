namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The node kinds of the regex syntax tree, shared by both dialects.</summary>
public enum RegexNodeKind
{
    /// <summary>Matches the empty string.</summary>
    Empty = 0,

    /// <summary>A single code point.</summary>
    Character,

    /// <summary>A character class: ranges, categories, blocks, negation, and subtraction resolved to one set.</summary>
    Set,

    /// <summary>A sequence of nodes.</summary>
    Concat,

    /// <summary>A choice between nodes.</summary>
    Alternation,

    /// <summary>A quantified node with a minimum, a maximum, and a laziness flag.</summary>
    Repeat,

    /// <summary>A group, capturing or not.</summary>
    Group,

    /// <summary>A wildcard.</summary>
    AnyChar,

    /// <summary>A back reference; XPath dialect only.</summary>
    BackReference,

    /// <summary>An anchor; XPath dialect only.</summary>
    Anchor,
}
