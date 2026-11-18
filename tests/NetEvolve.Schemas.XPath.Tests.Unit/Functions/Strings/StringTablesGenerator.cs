namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Emits <c>CaseMappingTables.generated.cs</c> and <c>NormalizationTables.generated.cs</c> from the pinned Unicode Character Database
/// files. See <c>src/NetEvolve.Schemas.XPath/Functions/Strings/README.md</c> for how to run it.
/// </summary>
internal static class StringTablesGenerator
{
    /// <summary>The environment variable that makes the generator test rewrite the committed files; the one the tables of the Xml package use.</summary>
    internal const string RegenerateVariable = "SCHEMAS_UCD_REGENERATE";

    private const int CodePointCount = 0x110000;
    private const int MaxExpansion = 3;

    internal static string FolderPath() =>
        Path.Combine(StringsUcd.RepositoryRoot(), "src", "NetEvolve.Schemas.XPath", "Functions", "Strings");

    internal static string CaseMappingPath() => Path.Combine(FolderPath(), "CaseMappingTables.generated.cs");

    internal static string NormalizationPath() => Path.Combine(FolderPath(), "NormalizationTables.generated.cs");

    /// <summary>Generates the case mapping tables: the simple mappings of <c>UnicodeData.txt</c>, overridden by the unconditional entries of <c>SpecialCasing.txt</c>.</summary>
    internal static string GenerateCaseMapping(string version, string[] unicodeData, string[] specialCasing)
    {
        var upper = new SortedDictionary<int, int[]>();
        var lower = new SortedDictionary<int, int[]>();
        foreach (var line in unicodeData)
        {
            var fields = line.Split(';');
            var codePoint = Hex(fields[0]);
            if (fields[12].Length > 0)
            {
                upper[codePoint] = [Hex(fields[12])];
            }

            if (fields[13].Length > 0)
            {
                lower[codePoint] = [Hex(fields[13])];
            }
        }

        foreach (var raw in specialCasing)
        {
            var content = WithoutComment(raw);
            if (content.Trim().Length == 0)
            {
                continue;
            }

            // code; lower; title; upper; (condition;)? - a row with a condition is conditional and not applied.
            var fields = content.Split(';');
            if (fields.Length > 4 && fields[4].Trim().Length > 0)
            {
                continue;
            }

            var codePoint = Hex(fields[0]);
            lower[codePoint] = List(fields[1]);
            upper[codePoint] = List(fields[3]);
        }

        var sb = Header(version);
        _ = sb.Append("namespace NetEvolve.Schemas.XPath.Functions.Strings;\n\n")
            .Append("internal static partial class CaseMappingTables\n{");
        AppendCaseDirection("Upper", upper, sb);
        AppendCaseDirection("Lower", lower, sb);
        _ = sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>Generates the normalization tables: combining classes, full decompositions, and the primary composites.</summary>
    internal static string GenerateNormalization(
        string version,
        string[] unicodeData,
        string[] derivedNormalizationProps
    )
    {
        var ccc = new byte[CodePointCount];
        var raw = new SortedDictionary<int, (bool Compat, int[] Mapping)>();
        foreach (var line in unicodeData)
        {
            var fields = line.Split(';');
            var codePoint = Hex(fields[0]);
            ccc[codePoint] = byte.Parse(fields[3], CultureInfo.InvariantCulture);
            if (fields[5].Length > 0)
            {
                var compat = fields[5].StartsWith('<');
                var text = compat
                    ? fields[5].Substring(fields[5].IndexOf('>', StringComparison.Ordinal) + 1)
                    : fields[5];
                raw[codePoint] = (compat, List(text));
            }
        }

        var excluded = new HashSet<int>();
        foreach (var line in derivedNormalizationProps)
        {
            var fields = WithoutComment(line).Split(';');
            if (
                fields.Length < 2
                || !string.Equals(fields[1].Trim(), "Full_Composition_Exclusion", StringComparison.Ordinal)
            )
            {
                continue;
            }

            var range = fields[0].Trim().Split("..", StringSplitOptions.None);
            var first = Hex(range[0]);
            var last = range.Length > 1 ? Hex(range[1]) : first;
            for (var cp = first; cp <= last; cp++)
            {
                _ = excluded.Add(cp);
            }
        }

        // Combining class runs: the first and last code point of each maximal run of one non-zero class.
        var cccFirst = new List<int>();
        var cccLast = new List<int>();
        var cccValue = new List<int>();
        for (var cp = 0; cp < CodePointCount; cp++)
        {
            if (ccc[cp] == 0)
            {
                continue;
            }

            if (cccValue.Count > 0 && cccLast[^1] == cp - 1 && cccValue[^1] == ccc[cp])
            {
                cccLast[^1] = cp;
            }
            else
            {
                cccFirst.Add(cp);
                cccLast.Add(cp);
                cccValue.Add(ccc[cp]);
            }
        }

        var canonicalKeys = new List<int>();
        var canonicalRefs = new List<int>();
        var canonicalData = new List<int>();
        var compatKeys = new List<int>();
        var compatRefs = new List<int>();
        var compatData = new List<int>();
        var pairs = new SortedDictionary<long, int>();
        foreach (var (codePoint, (compat, mapping)) in raw)
        {
            var canonical = Full(raw, codePoint, compat: false);
            var compatible = Full(raw, codePoint, compat: true);
            if (!compat)
            {
                Append(canonicalKeys, canonicalRefs, canonicalData, codePoint, canonical);
                if (mapping.Length == 2 && !excluded.Contains(codePoint))
                {
                    pairs[((long)mapping[0] << 32) | (uint)mapping[1]] = codePoint;
                }
            }

            if (!SameSequence(canonical, compatible))
            {
                Append(compatKeys, compatRefs, compatData, codePoint, compatible);
            }
        }

        var sb = Header(version);
        _ = sb.Append("namespace NetEvolve.Schemas.XPath.Functions.Strings;\n\n")
            .Append("internal static partial class NormalizationTables\n{");
        AppendInts("CombiningFirst", cccFirst, sb);
        AppendInts("CombiningLast", cccLast, sb);
        AppendBytes("CombiningValue", cccValue, sb);
        AppendInts("CanonicalKeys", canonicalKeys, sb);
        AppendInts("CanonicalRefs", canonicalRefs, sb);
        AppendInts("CanonicalData", canonicalData, sb);
        AppendInts("CompatibilityKeys", compatKeys, sb);
        AppendInts("CompatibilityRefs", compatRefs, sb);
        AppendInts("CompatibilityData", compatData, sb);
        AppendLongs("CompositionKeys", [.. pairs.Keys], sb);
        AppendInts("CompositionValues", [.. pairs.Values], sb);
        _ = sb.Append("}\n");
        return sb.ToString();
    }

    private static string WithoutComment(string line)
    {
        var hash = line.IndexOf('#', StringComparison.Ordinal);
        return hash < 0 ? line : line.Substring(0, hash);
    }

    // The full decomposition: every code point replaced by its mapping, recursively; a compatibility mapping only when asked for.
    private static List<int> Full(SortedDictionary<int, (bool Compat, int[] Mapping)> raw, int codePoint, bool compat)
    {
        var result = new List<int>();
        Expand(raw, codePoint, compat, result);
        return result;
    }

    private static void Expand(
        SortedDictionary<int, (bool Compat, int[] Mapping)> raw,
        int codePoint,
        bool compat,
        List<int> result
    )
    {
        if (raw.TryGetValue(codePoint, out var entry) && (compat || !entry.Compat))
        {
            foreach (var part in entry.Mapping)
            {
                Expand(raw, part, compat, result);
            }
        }
        else
        {
            result.Add(codePoint);
        }
    }

    private static bool SameSequence(List<int> left, List<int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    // A reference packs the offset into the data and the length: (offset << 5) | length, with at most 31 code points.
    private static void Append(List<int> keys, List<int> refs, List<int> data, int codePoint, List<int> mapping)
    {
        if (mapping.Count >= 32)
        {
            throw new InvalidOperationException($"The decomposition of U+{codePoint:X4} is too long to pack.");
        }

        keys.Add(codePoint);
        refs.Add((data.Count << 5) | mapping.Count);
        data.AddRange(mapping);
    }

    private static void AppendCaseDirection(string name, SortedDictionary<int, int[]> map, StringBuilder sb)
    {
        var keys = new List<int>();
        var values = new List<int>();
        var expansions = new List<int>();
        foreach (var (codePoint, target) in map)
        {
            if (target.Length == 1 && target[0] == codePoint)
            {
                continue;
            }

            if (target.Length > MaxExpansion)
            {
                throw new InvalidOperationException(
                    $"The {name} mapping of U+{codePoint:X4} has {target.Length} code points."
                );
            }

            keys.Add(codePoint);
            if (target.Length == 1)
            {
                values.Add(target[0]);
            }
            else
            {
                // A negative value is an expansion: the complement of (offset << 2) | (count - 1).
                values.Add(~((expansions.Count << 2) | (target.Length - 1)));
                expansions.AddRange(target);
            }
        }

        AppendInts(name + "Keys", keys, sb);
        AppendInts(name + "Values", values, sb);
        AppendInts(name + "Expansions", expansions, sb);
    }

    private static StringBuilder Header(string version)
    {
        var sb = new StringBuilder();
        _ = sb.Append("// <auto-generated>\n")
            .Append("//   Generated from the Unicode Character Database ")
            .Append(version)
            .Append(" pinned in tests/conformance/suites/ucd.json.\n")
            .Append("//   Do not edit by hand; see README.md in this folder for how to regenerate.\n")
            .Append("// </auto-generated>\n\n");
        return sb;
    }

    private static int Hex(string text) => int.Parse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static int[] List(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            result[i] = Hex(parts[i]);
        }

        return result;
    }

    private static void AppendInts(string name, List<int> values, StringBuilder sb)
    {
        _ = sb.Append("\n    internal static readonly int[] ").Append(name).Append(" =\n    [");
        for (var i = 0; i < values.Count; i++)
        {
            _ = sb.Append(i % 16 == 0 ? "\n        " : " ");
            _ = sb.Append(
                values[i] < 0
                    ? "-0x" + (-(long)values[i]).ToString("X", CultureInfo.InvariantCulture)
                    : "0x" + values[i].ToString("X", CultureInfo.InvariantCulture)
            );
            _ = sb.Append(',');
        }

        _ = sb.Append("\n    ];\n");
    }

    private static void AppendBytes(string name, List<int> values, StringBuilder sb)
    {
        _ = sb.Append("\n    internal static readonly byte[] ").Append(name).Append(" =\n    [");
        for (var i = 0; i < values.Count; i++)
        {
            _ = sb.Append(i % 24 == 0 ? "\n        " : " ");
            _ = sb.Append(values[i].ToString(CultureInfo.InvariantCulture)).Append(',');
        }

        _ = sb.Append("\n    ];\n");
    }

    private static void AppendLongs(string name, List<long> values, StringBuilder sb)
    {
        _ = sb.Append("\n    internal static readonly long[] ").Append(name).Append(" =\n    [");
        for (var i = 0; i < values.Count; i++)
        {
            _ = sb.Append(i % 8 == 0 ? "\n        " : " ");
            _ = sb.Append("0x").Append(values[i].ToString("X", CultureInfo.InvariantCulture)).Append("L,");
        }

        _ = sb.Append("\n    ];\n");
    }
}
