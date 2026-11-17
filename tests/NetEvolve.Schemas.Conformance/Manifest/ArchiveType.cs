namespace NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The archive format of a pinned conformance suite.</summary>
public enum ArchiveType
{
    /// <summary>A zip archive.</summary>
    Zip,

    /// <summary>A gzip-compressed tar archive (<c>.tar.gz</c>).</summary>
    TarGz,
}
