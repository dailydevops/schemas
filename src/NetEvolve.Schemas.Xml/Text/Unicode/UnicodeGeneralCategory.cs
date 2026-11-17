namespace NetEvolve.Schemas.Xml.Text.Unicode;

/// <summary>The 30 Unicode general categories (the two-letter values of <c>UnicodeData.txt</c> field 2).</summary>
/// <remarks>
/// Own enumeration on purpose: the runtime's <see cref="System.Globalization.UnicodeCategory"/> follows the Unicode
/// version of the running .NET, while these values follow the one pinned version (<see cref="UnicodeTables.Version"/>).
/// </remarks>
internal enum UnicodeGeneralCategory : byte
{
    /// <summary>Letter, uppercase.</summary>
    Lu,

    /// <summary>Letter, lowercase.</summary>
    Ll,

    /// <summary>Letter, titlecase.</summary>
    Lt,

    /// <summary>Letter, modifier.</summary>
    Lm,

    /// <summary>Letter, other.</summary>
    Lo,

    /// <summary>Mark, nonspacing.</summary>
    Mn,

    /// <summary>Mark, spacing combining.</summary>
    Mc,

    /// <summary>Mark, enclosing.</summary>
    Me,

    /// <summary>Number, decimal digit.</summary>
    Nd,

    /// <summary>Number, letter.</summary>
    Nl,

    /// <summary>Number, other.</summary>
    No,

    /// <summary>Punctuation, connector.</summary>
    Pc,

    /// <summary>Punctuation, dash.</summary>
    Pd,

    /// <summary>Punctuation, open.</summary>
    Ps,

    /// <summary>Punctuation, close.</summary>
    Pe,

    /// <summary>Punctuation, initial quote.</summary>
    Pi,

    /// <summary>Punctuation, final quote.</summary>
    Pf,

    /// <summary>Punctuation, other.</summary>
    Po,

    /// <summary>Symbol, math.</summary>
    Sm,

    /// <summary>Symbol, currency.</summary>
    Sc,

    /// <summary>Symbol, modifier.</summary>
    Sk,

    /// <summary>Symbol, other.</summary>
    So,

    /// <summary>Separator, space.</summary>
    Zs,

    /// <summary>Separator, line.</summary>
    Zl,

    /// <summary>Separator, paragraph.</summary>
    Zp,

    /// <summary>Other, control.</summary>
    Cc,

    /// <summary>Other, format.</summary>
    Cf,

    /// <summary>Other, surrogate (U+D800 to U+DFFF).</summary>
    Cs,

    /// <summary>Other, private use.</summary>
    Co,

    /// <summary>Other, not assigned (also every noncharacter and every code point outside U+0000 to U+10FFFF).</summary>
    Cn,
}
