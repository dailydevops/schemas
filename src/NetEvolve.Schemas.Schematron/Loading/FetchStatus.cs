namespace NetEvolve.Schemas.Schematron.Loading;

/// <summary>The outcome of fetching the resource of one include.</summary>
internal enum FetchStatus : byte
{
    /// <summary>The content was read.</summary>
    Found = 0,

    /// <summary>A resource error: for XInclude 1.0 the <c>xi:fallback</c> element recovers from it.</summary>
    Recoverable = 1,

    /// <summary>A limit was breached: no fallback recovers from it.</summary>
    Fatal = 2,
}
