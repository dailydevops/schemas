namespace NetEvolve.Schemas.Conformance.Manifest;

/// <summary>A pinned conformance suite: where to download it and the checksum it must match.</summary>
/// <param name="Key">The suite key, unique across all manifests.</param>
/// <param name="SourceUrl">The absolute http or https URL of the archive.</param>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 checksum (FIPS 180-4) of the archive.</param>
/// <param name="ArchiveType">The archive format.</param>
/// <param name="SubPath">An optional relative directory inside the archive that is the suite root.</param>
public sealed record SuiteManifest(
    string Key,
    Uri SourceUrl,
    string Sha256,
    ArchiveType ArchiveType,
    string? SubPath = null
);
