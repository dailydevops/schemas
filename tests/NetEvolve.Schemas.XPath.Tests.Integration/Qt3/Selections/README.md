# QT3 selections for XPath 3.0 and 3.1

The selections decide which cases of the pinned QT3 suite (`tests/conformance/suites/qt3.json`) apply to XPath 3.0 and to XPath 3.1.
They sit on top of the XPath 2.0 selection of `Qt3/Runner/Qt3Selector.cs`, which stays unchanged.

## Rules

- **Spec tokens.** XPath 3.0 selects `XP10+`, `XP20+`, `XP30`, and `XP30+`. XPath 3.1 selects `XP10+`, `XP20+`, `XP30+`, `XP31`, and `XP31+`.
  Set-level and case-level dependencies are combined with AND; `satisfied="false"` negates a dependency.
- **Exclusion list.** `Qt3Xp3Exclusions` holds one entry per dependency, test set, or function pattern, each with a reason and, where a
  deferred feature is the reason, its DEF ID and issue: DEF-40 to DEF-49, DEF-68, DEF-69, DEF-73, DEF-74, and DEF-76. Three entries
  (XML 1.1, XSD 1.1, and a host collation) repeat the rules of the XPath 2.0 selection and name no deferred feature. The excluded cases
  keep their reason in the selection result.
- **Function patterns.** A case outside an excluded test set that uses an excluded function or collation (for example through
  `fn:function-lookup`) is excluded by the pattern of that function. The text checked is the query and the expected result, never the
  description.
- **Counts.** `selected-xp30.txt` and `selected-xp31.txt` record the number of selected cases per test set, and the tests pin the totals
  and the suite checksum. A changed count or a re-pinned suite fails a test. Rewrite the files with the explicit test
  `Qt3Xp3BaselineTests.Counts_Regenerate` after a deliberate change.

## Baselines

The baseline of a tier lists the cases it records that do not pass, failed or skipped, as `<test-set>/<case-name>`:

| Tier   | Directory                                | Cases recorded                                                  |
| ------ | ---------------------------------------- | --------------------------------------------------------------- |
| XP 2.0 | `tests/conformance/baselines/qt3`        | The XPath 2.0 selection (unchanged).                            |
| XP 3.0 | `tests/conformance/baselines/qt3-xp30`   | Selected for XPath 3.0 and not by XPath 2.0.                    |
| XP 3.1 | `tests/conformance/baselines/qt3-xp31`   | Selected for XPath 3.1 and by neither XPath 2.0 nor XPath 3.0.  |

A case that several selections share is recorded once, in the first tier that selects it, so a package that fixes it edits one file.
`Qt3Xp3ConformanceTests` runs one test per test set and tier, and it fails on a regression and on a listed case that passes.
Regenerate a tier with the explicit test `Qt3Xp3ConformanceTests.Baseline_Regenerate`.

## Limitations

- The engine compiles every query as XPath 2.0, because the later versions are not enabled. A case that needs XPath 3.x syntax or a
  3.x function is a known failure until the package that adds it removes the case from the baseline.
- Cases that apply to XQuery or XSLT only are not applicable to an XPath processor. They are excluded with that reason, and no DEF issue
  covers them.
- The exclusion list is preliminary until XP-73 closes it. The rates of the two selections are reported there, not here.
