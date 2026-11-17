namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Occurrence indicator of a sequence type.</summary>
internal enum Occurrence
{
    /// <summary>No indicator.</summary>
    ExactlyOne = 0,

    /// <summary><c>?</c></summary>
    ZeroOrOne = 1,

    /// <summary><c>*</c></summary>
    ZeroOrMore = 2,

    /// <summary><c>+</c></summary>
    OneOrMore = 3,
}
