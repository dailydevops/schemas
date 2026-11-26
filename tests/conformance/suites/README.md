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
cases of `xslt-3` are excluded with a reason (SCH-21). Every selected case runs once per selected binding, with the `queryBinding` of
the schema set to the binding (`xslt1`, `xslt2`); a test set is named `<binding>.<case folder>` and has its own baseline file.

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

## W3C XInclude test suite (`xinclude.json`)

The W3C XInclude test suite (`XIncl20060927.zip`, a static W3C file; W3C test suite licence) is pinned by checksum. The archive
holds CVS folders, which the runner never reads. The runner lives in
`tests/NetEvolve.Schemas.Schematron.Tests.Unit/Loading/XInclude/Suite` and drives the internal XInclude resolver of the Schematron
loader, because the resolver is not public API.

- **Catalog:** `testdescr.xml` lists 174 cases of five contributors (`Imaq`, `FourThought`, `Nist`, `EdUni`, `Harold`). A case has
  the type `success` or `error`, optional `features`, and a reference result under `result/`.
- **Comparison:** the resolver reads every resource through a sandboxed file resolver rooted at the suite. A `success` case passes
  when the merged tree equals the reference result: names compare as expanded names, attributes in ascending order, and
  namespace declarations and the document type are ignored. An `error` case passes when the input is not well formed or the
  resolver reports a problem.
- **Exclusions:** cases with the feature `xpointer-scheme`, `unexpanded-entities`, or `unparsed-entities`, and the four cases
  that read a live http resource, are skipped with their reason.
- **Baseline:** `tests/conformance/baselines/xinclude/<contributor>.txt` lists the failed cases of one contributor folder.
  `known-failure-links.tsv` links each entry to its issue and reason, separated by tabs; an entry without a line, or a line
  without an entry, fails the test.
- **Run:** `XIncludeConformanceTests` is explicit and in the `Conformance` category; the baseline guard
  `XIncludeKnownFailureTests` needs no download.

## libxml2 regression corpus (`libxml2.json`)

The libxml2 source archive is pinned by tag (`v2.15.4`, MIT licence): `https://github.com/GNOME/libxml2/archive/refs/tags/v2.15.4.tar.gz`
with the `subPath` `libxml2-2.15.4`. Only three sub-trees whose expectations do not depend on libxml2 output formats are run. Pairs
of input and expectation are matched by path convention, because the corpus has no catalog.

- **Byte stability:** GitHub generates tag archives on request and does not promise byte-identical output over time (two
  downloads on 2026-11-26 had the same SHA-256). If it changes, re-pin as described for QT3.
- **URI (`test/URI/uri.data`, `result/URI/uri.data`):** each line is resolved against `http://foo.com/path/to/index.html?orig#help`
  with the internal reference resolver and compared with the result line; `::ERROR::` expects a refused reference. The parse-only
  files `*.uri` (no base, normalization only) are not run. The runner lives in `tests/NetEvolve.Schemas.Tests.Unit/Resources/Libxml2`.
- **Regexp (`test/regexp/*`, `result/regexp/*`):** every pattern runs on the XSD dialect of the regex engine. A line with `=>` (or the
  first line of a file) is a pattern that must compile or, when the result says `failed to compile`, must not; the lines that follow
  are inputs that must match as a whole (`Ok`) or must not (`Fail`). As in the libxml2 driver, blank lines and `#` lines are skipped
  and a pattern that does not compile ends the file. The case id is `<file>/<pattern>/<input>` (one-based). The runner lives in
  `tests/NetEvolve.Schemas.Xml.Tests.Integration/Libxml2`.
- **Errors (`test/errors/*.xml`):** a document must be rejected by the pipeline reader (DTD processing on, no external access)
  when its message file `result/errors/<name>.err` holds a parser, namespace, or I/O error; a document with warnings only, or
  without a message file, must be accepted. The `.ent` and `.str` result variants, `test/errors10`, and the message texts are not
  compared (libxml2-specific).
- **Baselines:** `tests/conformance/baselines/libxml2-<tree>/<test set>.txt` lists known failures; the URI and regexp trees have none.
  `libxml2-errors/known-failure-links.tsv` gives each entry `#issue` (engine defect) or `leniency` (deliberate difference from
  libxml2) and its reason, separated by tabs; an entry without a line, or a line without an entry, fails the guard
  `Libxml2KnownFailureTests`, which needs no download.
- **Run:** the tests in `Libxml2ConformanceTests` and `Libxml2UriConformanceTests` are explicit and in the `Conformance` category;
  `Baseline_Regenerate` rewrites the baseline files after a deliberate change.
- **Out of scope:** `test/c14n` (Canonical XML is not implemented), `test/xmlid`, SAX dumps, and message files.
