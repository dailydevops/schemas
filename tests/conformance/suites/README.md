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

## QT3 (`qt3.json`)

The W3C QT3 test suite is pinned by commit, not by branch. The archive is the GitHub commit-SHA archive
`https://github.com/w3c/qt3tests/archive/<commit>.zip`; the manifest carries its SHA-256 and the `subPath`
`qt3tests-<commit>`. The runner lives in `tests/NetEvolve.Schemas.XPath.Tests.Integration/Qt3`.

- **Limitation, byte stability:** GitHub generates these archives on request and does not promise
  byte-identical output over time. If GitHub changes its archive format, the fetch fails with a checksum
  mismatch even though the commit is unchanged. Re-pin by downloading the archive for the same commit,
  verifying its content against the commit, and updating `sha256`.
- **Selection:** XPath 2.0 cases (`XP20`, `XP20+`, `XP10+`). XQuery and XSLT cases are never selected.
  Cases that need XSD 1.1, XML 1.1, schema import, schema validation, static typing, the namespace axis, or
  a non-codepoint collation are excluded; each exclusion keeps its reason in the selection result.
- **Baseline:** `tests/conformance/baselines/qt3/<test-set>.txt` lists every selected case of one test set as
  `<test-set>/<case-name>`, one per line. Test sets without a selected case have no file. Regenerate the files
  with the explicit test `Qt3BaselineTests.Baseline_Regenerate` after a deliberate filter change.
