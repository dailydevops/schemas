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

A `*.json` file in this folder that is a JSON object declaring none of `key`, `url`, `sha256` or `archive`
(for example the stage record `stage-sources.json`) is not a manifest and is skipped by the directory load.
Any file that declares at least one of these fields is a manifest and is validated in full.

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
- **Engine:** `Qt3Engine` evaluates each case with the public XPath API. It loads the environment of the case
  (the sources, the namespace bindings, the parameters, and the static base URI; defined in the case, the test
  set, or the catalog) with the Xml document loader and binds the context item and the variables.
- **Assertions:** `error`, `assert-true`, `assert-false`, `assert-empty`, `assert`, `assert-eq`, `assert-count`,
  `assert-string-value`, `assert-deep-eq`, `assert-permutation`, `assert-type`, and `assert-xml` decide a case.
  Every other kind (the serialization assertions and the compound `any-of`, `all-of`, and `not`) is skipped and
  never counts as passed. `assert-xml` writes the result and the expected XML through the canonical XML writer and
  compares the two texts; `ignore-prefixes` compares them without prefixes and namespace declarations, and the
  expected XML of a `file` attribute is read as a fragment. The test `StructuralSuiteTests` reports, per test set,
  how many of these cases moved from skipped to passed or failed.
- **Limitations:** the schema, collection, resource, collation, decimal-format, function-library, and
  context-item elements of an environment are ignored. The static base URI is absent unless the environment
  gives one, so a result never depends on the checkout path. A case whose query raises an engine condition that is
  no error of the specifications (a limit, a document that does not load, an unexpected exception) fails, also
  where the case expects `error code="*"`.
- **Baseline:** `tests/conformance/baselines/qt3/<test-set>.txt` lists every selected case of one test set that
  does not pass, failed or skipped, as `<test-set>/<case-name>`, one per line. A case that passes but is still
  listed fails the run, so a baseline only shrinks on purpose. Test sets without a selected case have no file.
  Regenerate the files with the explicit test `Qt3BaselineTests.Baseline_Regenerate` after a deliberate change.
- **Tiers:** `qt3.json` also feeds the XPath 3.0 and 3.1 tiers (`qt3-xp30` and `qt3-xp31`). Their selections, counts, and baselines
  (`tests/conformance/baselines/qt3-xp30` and `qt3-xp31`) are described in
  `tests/NetEvolve.Schemas.XPath.Tests.Integration/Qt3/Selections/README.md`.

## Suites without releases

Some suites publish no tags or releases, so there is no release asset to pin. `xsts.json` pins the commit
archive of the suite repository (`.../archive/<commit-sha>.zip`) instead. The commit is immutable, and the
archive is the file that GitHub generates for it on request.

Limitation: GitHub does not promise byte-identical generated archives over time. A change of its archive
tool would change the SHA-256 and make the fetch fail with a checksum mismatch that names the suite key; the
content of the commit is unaffected. In that case, download the archive for the same commit again, verify
the extracted content against the commit, and update the checksum in the manifest.

`xsts.json` pins the W3C XML Schema Test Suite at the head of the default branch (commit `7bc3365c`) because
the repository has neither tags nor releases, and the dated 2007 release contains XSD 1.0 tests only.

## XML Conformance Test Suite (`xmlconf.json`)

The W3C XML Conformance Test Suite is pinned by its official release archive,
`https://www.w3.org/XML/Test/xmlts20130923.zip`, with the `subPath` `xmlconf`. The runner lives in
`tests/NetEvolve.Schemas.Xml.Tests.Integration/XmlConf`.

- **Byte stability:** the archive is a static file on the W3C server, so its bytes do not change between downloads
  (two independent downloads produced the same SHA-256). If the W3C ever replaces the file, the fetch fails with a
  checksum mismatch; re-pin by downloading the new release and updating `sha256`.
- **Selection:** every case of the catalog `xmlconf.xml` except cases for XML 1.1 (`RECOMMENDATION` `XML1.1`, `VERSION`
  without `1.0`), for Namespaces in XML 1.1, and cases that apply only to XML 1.0 editions one to four (`EDITION`
  without `5`). Each exclusion keeps its reason in the selection result. Every selected case carries its `ENTITIES`
  tag (`none`, `general`, `parameter`, `both`) and its `NAMESPACE` flag.
- **Outcome rules:** a `not-wf` case passes only when the reader reports a fatal error; `valid` and `invalid` cases
  pass only when it reports none (a non-validating reader accepts an invalid document); an `error` case passes with
  any observation of a bound reader, because readers are not required to report it. Without a bound reader (XML-53)
  every selected case is a known failure.
- **Baseline:** `tests/conformance/baselines/xmlconf/<test-set>.txt` lists every selected case of one test set as
  `<test-set>/<case-id>`, one per line. The test set is the effective `xml:base` of the case with `/` replaced by
  `-`. Regenerate the files with the explicit test `XmlConfSuiteTests.Baseline_Regenerate` after a deliberate filter
  change.

## Schematron conformance suite (`schematron-conformance.json`)

The Schematron conformance suite is pinned by release tag (`v2.0`); the runner lives in
`tests/NetEvolve.Schemas.Schematron.Tests.Integration/Conformance`. The bindings `xslt-1` and `xslt-2` are selected, and the
cases of `xslt-3` are excluded with a reason (SCH-21).

- **Comparison:** the suite states its expectations as XPath assertions on the SVRL output, not as expected SVRL files. The engine
  validates each case with the public `SchemaValidator.ForSchematron` entry point and the SVRL report sink, loads the SVRL output,
  and evaluates the assertions on the XPath engine. The outcome is `error` for a schema that does not compile or an `svrl:error`,
  `invalid` for a failed assertion or a successful report, and `valid` otherwise. The comparison ignores the order of attributes,
  white space between elements, and the prefix of the SVRL namespace, and nothing else.
- **Baseline:** `tests/conformance/baselines/schematron-conformance/<test set>.txt` lists the known failures. Each entry has a line in
  `tests/NetEvolve.Schemas.Schematron.Tests.Integration/Conformance/Engine/known-failure-links.txt` with its issue and its reason,
  separated by tabs; a baseline entry without a line, or a line without an entry, fails the test.
- **Limitation:** the case reader keeps the text of an assertion and not its namespace declarations, so the engine declares the prefix
  `svrl` for the assertions; every assertion of the pinned release uses this prefix only.

## XSpec Schematron scenarios (`xspec.json`)

The XSpec project is pinned by its release tag archive (`v4.1.2`, MIT license) with the `subPath` `xspec-4.1.2`. The reader and the
runner live in `tests/NetEvolve.Schemas.Schematron.Tests.Integration/Conformance/Xspec`; the class `XspecConformanceTests` is explicit and
selected by the conformance category.

- **Selection:** every `.xspec` file below `test` and `tutorial` whose path names Schematron. A test set is one such file, named by its path
  with `/` replaced by `-` and without the extension. A case is one declarative expectation: `x:expect-assert`, `x:expect-not-assert`,
  `x:expect-report`, `x:expect-not-report`, or `x:expect-valid`, with `id`, `role`, `location`, `count`, and text content. `x:import` adds the
  scenarios of the imported file, and the parameter `phase` (also `schxslt:phase`) selects the phase.
- **Exclusions:** every skipped file, scenario, or expectation keeps its reason in the selection result. Left out are everything that needs
  an XSLT or XQuery processor (`x:variable`, `x:call`, `x:helper`, `x:expect`, `x:like`, `x:expect-rule`, other parameters, `x:context/@select`,
  placeholders and value templates in a context), `pending` and `shared` scenarios, a file without a `schematron` attribute, a file that is not
  well formed XML 1.0, and the files listed in `XspecReader.ExcludedPaths`, which test XSpec itself (error handling, pending, focus, and
  failing expectations on purpose).
- **Outcome rules:** the SVRL output decides, as in XSpec. An id or role is the one of the assertion, else the one of the nearest preceding
  rule, else the one of the nearest preceding pattern. A location is an XPath expression whose selected node must equal the node of the
  location in the SVRL. The negative expectations and `x:expect-valid` also require that a rule fired. `x:expect-valid` fails on a failed
  assertion or a successful report without a role or with the role `error` or `fatal`, and tolerates every other role.
- **Baseline:** `tests/conformance/baselines/xspec/<test set>.txt` lists the known failures of one `.xspec` file as `<test set>/<case>`, one per
  line, and only files with failures have one. Each entry has a line in
  `tests/NetEvolve.Schemas.Schematron.Tests.Integration/Conformance/Xspec/known-failure-links.txt` with its issue and its reason, separated by
  tabs. Regenerate the files with the explicit test `XspecConformanceTests.Baseline_Regenerate` after a deliberate change.
- **Limitation:** XSpec compiles the schema to XSLT and runs the scenario on it; here the schema runs on the library validator. A scenario that
  XSpec reports as failed on purpose, or that checks XSpec itself, is excluded, not counted as a failure of the library.
