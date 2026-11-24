namespace NetEvolve.Schemas.XPath.Formatting.Integers;

/// <summary>The numbering sequence a picture names.</summary>
internal enum IntegerSequence
{
    /// <summary>A decimal digit pattern, or the fallback token <c>1</c>.</summary>
    Decimal,

    /// <summary><c>a</c>.</summary>
    AlphaLower,

    /// <summary><c>A</c>.</summary>
    AlphaUpper,

    /// <summary><c>i</c>.</summary>
    RomanLower,

    /// <summary><c>I</c>.</summary>
    RomanUpper,

    /// <summary><c>w</c>.</summary>
    WordsLower,

    /// <summary><c>W</c>.</summary>
    WordsUpper,

    /// <summary><c>Ww</c>.</summary>
    WordsTitle,
}
