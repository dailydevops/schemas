namespace NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary>A normalization form of <c>fn:normalize-unicode</c> (Unicode Standard Annex 15).</summary>
/// <remarks>Limitation: the fully-normalized form is not supported (DEF-75, #87); it is not a member.</remarks>
internal enum UnicodeNormalizationForm
{
    /// <summary>Canonical decomposition, then canonical composition.</summary>
    Nfc = 0,

    /// <summary>Canonical decomposition.</summary>
    Nfd = 1,

    /// <summary>Compatibility decomposition, then canonical composition.</summary>
    Nfkc = 2,

    /// <summary>Compatibility decomposition.</summary>
    Nfkd = 3,
}
