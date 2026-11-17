# Conformance suite manifests

Conformance suites and rule sets are never committed. Each suite is pinned by one manifest file in this
folder, `tests/conformance/suites/<suite>.json`, and fetched on demand by pinned checksum. The loader and
fetch step live in `tests/NetEvolve.Schemas.Conformance`.

## Manifest fields

| Field     | Required | Description                                                                              |
| --------- | -------- | ---------------------------------------------------------------------------------------- |
| `key`     | yes      | Suite key. A plain name (no path separators), unique across all manifests in this folder. |
| `url`     | yes      | Absolute `http` or `https` URL of the archive.                                           |
| `sha256`  | yes      | SHA-256 (FIPS 180-4) of the archive as 64 hexadecimal characters, case-insensitive.      |
| `archive` | yes      | Archive type: `zip` or `tar.gz`.                                                         |
| `subPath` | no       | Relative directory inside the archive that is the suite root. Must not leave the archive. |

```json
{
  "key": "example-suite",
  "url": "https://example.invalid/suite-1.2.3.zip",
  "sha256": "0000000000000000000000000000000000000000000000000000000000000000",
  "archive": "zip",
  "subPath": "suite-1.2.3/tests"
}
```

A manifest with a missing or malformed field is rejected when it is loaded, and two files that declare the
same `key` fail the load.

## How to pin a suite

1. Download the archive once from its official release location.
2. Compute its SHA-256, for example `sha256sum archive.zip` or `Get-FileHash -Algorithm SHA256 archive.zip`.
3. Add `<suite>.json` with the fields above. Pin a release, never a moving branch archive.
4. Commit only the manifest. The archive itself is never committed.

## Fetch and cache

- The fetch downloads the archive, verifies the checksum, and only then extracts it. A checksum mismatch
  fails with an error that names the suite key, and nothing is extracted.
- Extraction rejects entries with an absolute path, entries that leave the target directory, and tar links.
- The cache is keyed by checksum: a second fetch with a filled cache performs no download.
- The cache folder is the environment variable `SCHEMAS_CONFORMANCE_CACHE` when set, otherwise the git-ignored
  `artifacts/conformance-cache` folder in the repository.
