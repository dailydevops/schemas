# Embedded Unicode tables

Lookup tables for the Unicode general category, the block, and the simple case folding of a code point, embedded
in the Xml package so the regex engine gives the same answers on every target framework (`netstandard2.0`,
`netstandard2.1`, `net10.0`, `net11.0`). The runtime's own Unicode data is never consulted.

## Pinned version

**Unicode 18.0.0.** The version is exposed as `UnicodeTables.Version`. Results follow this one version, not the
Unicode version of the running .NET. The Unicode Character Database files are pinned by checksum in
`tests/conformance/suites/ucd.json` (the official `UCD.zip` of that release); they are fetched on demand and
never committed.

## API (internal)

| Member                                    | Result                                                                                  |
| ----------------------------------------- | --------------------------------------------------------------------------------------- |
| `GetCategory(int)`                        | `UnicodeGeneralCategory` of a code point                                                |
| `GetBlock(int)` / `GetBlockName(int)`     | Position of the block in `Blocks.txt` (`NoBlock` for `No_Block`) and its name           |
| `FindBlock(ReadOnlySpan<char>)`           | Handle for a normalized block name (spaces and underscores removed, hyphens and case kept) |
| `IsInBlock(int handle, int codePoint)`    | Whether a code point is in the named block                                              |
| `SimpleCaseFold(int)`                     | Simple case folding (`C` and `S` entries of `CaseFolding.txt`)                          |

All lookups are allocation-free and accept any `int`; a value outside U+0000 to U+10FFFF is unassigned (`Cn`), in
no block, and folds to itself.

## Documented results and limitations

- Unassigned code points and the noncharacters (U+FDD0 to U+FDEF, U+nFFFE, U+nFFFF) are `Cn`.
- Surrogates (U+D800 to U+DFFF) are `Cs`; private use code points are `Co`, as in `UnicodeData.txt`.
- Case folding is the simple folding only. Full foldings (`F`, for example U+00DF to `ss`) and the Turkic foldings
  (`T`) are not applied.
- Block names are the `Blocks.txt` names. `FindBlock` additionally knows the superseded names that XSD 1.1 Part 2
  (G.4.2.3) cites as alias rows: `Greek` (U+0370 to U+03FF), `CombiningMarksforSymbols` (U+20D0 to U+20FF), and
  `PrivateUse` (U+E000 to U+F8FF, U+F0000 to U+FFFFD, U+100000 to U+10FFFD, the ranges as cited there), plus the
  Blocks 4.0.0 name `CyrillicSupplementary` (U+0500 to U+052F). The fixtures `Fixtures/Blocks-3.1.txt` and
  `Fixtures/Blocks-4.0.0.txt` in the unit-test project pin which names exist; a test resolves every one of them.

## Regenerating the tables

`UnicodeTables.generated.cs` is generated from the pinned files by `UnicodeTableGenerator` in
`tests/NetEvolve.Schemas.Xml.Tests.Unit/Text/Unicode`. To move to a new Unicode version:

1. Download the new `UCD.zip` from `https://www.unicode.org/Public/<version>/ucd/` and compute its SHA-256.
2. Update `url` and `sha256` in `tests/conformance/suites/ucd.json`, and `UnicodeTables.Version`.
3. Run the generator test with the variable set, which rewrites the file instead of only comparing it:

   ```shell
   SCHEMAS_UCD_REGENERATE=1 dotnet test --project tests/NetEvolve.Schemas.Xml.Tests.Unit -f net10.0 \
     --treenode-filter "/*/*/*/CommittedTables_AreReproducibleFromThePinnedFiles"
   ```

4. Run the whole test project. The tests compare every code point against the pinned files; without the variable the
   same test fails whenever the committed file differs from the generator output.
