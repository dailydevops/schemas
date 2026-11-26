namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

/// <summary>Pairs the lines of a libxml2 <c>test/regexp</c> file with the verdicts of its <c>result/regexp</c> file.</summary>
internal static class Libxml2RegexCatalog
{
    private static readonly char[] LineEnd = ['\n', '\t', '\r', ' '];

    /// <summary>Reads every file of the <c>test/regexp</c> folder.</summary>
    /// <param name="root">The libxml2 source root.</param>
    /// <returns>The cases in file and line order.</returns>
    public static List<Libxml2RegexCase> Read(string root)
    {
        var cases = new List<Libxml2RegexCase>();
        var names = Directory
            .GetFiles(Path.Combine(root, "test", "regexp"))
            .Select(f => Path.GetFileName(f))
            .OrderBy(n => n, StringComparer.Ordinal);
        foreach (var name in names)
        {
            cases.AddRange(
                Parse(
                    name,
                    File.ReadAllText(Path.Combine(root, "test", "regexp", name)),
                    File.ReadAllText(Path.Combine(root, "result", "regexp", name))
                )
            );
        }

        return cases;
    }

    /// <summary>Parses one file the way the libxml2 test driver reads it.</summary>
    /// <param name="file">The file name.</param>
    /// <param name="test">The text of the test file.</param>
    /// <param name="result">The text of the result file.</param>
    /// <returns>The cases of the file.</returns>
    /// <exception cref="InvalidDataException">The result file does not match the test file.</exception>
    public static List<Libxml2RegexCase> Parse(string file, string test, string result)
    {
        var expected = ParseResult(result);
        var cases = new List<Libxml2RegexCase>();
        var pattern = 0;
        string? current = null;
        var input = 0;
        foreach (var raw in test.Split('\n'))
        {
            var line = raw.TrimEnd(LineEnd);
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var isPattern = line.StartsWith("=>", StringComparison.Ordinal);
            if (isPattern || current is null)
            {
                current = isPattern ? line[2..] : line;
                pattern++;
                input = 0;
                if (pattern > expected.Count)
                {
                    throw new InvalidDataException($"{file}: no verdict block for pattern {pattern}.");
                }

                cases.Add(
                    new Libxml2RegexCase($"{file}/{pattern}", file, current, null, expected[pattern - 1].Compiles)
                );
                if (!expected[pattern - 1].Compiles)
                {
                    break; // the libxml2 driver stops at the first pattern that does not compile
                }

                continue;
            }

            input++;
            var verdicts = expected[pattern - 1].Verdicts;
            if (input > verdicts.Count)
            {
                throw new InvalidDataException($"{file}: no verdict for input {input} of pattern {pattern}.");
            }

            cases.Add(new Libxml2RegexCase($"{file}/{pattern}/{input}", file, current, line, verdicts[input - 1]));
        }

        return pattern == expected.Count
            ? cases
            : throw new InvalidDataException($"{file}: {pattern} patterns but {expected.Count} verdict blocks.");
    }

    private static List<(bool Compiles, List<bool> Verdicts)> ParseResult(string result)
    {
        var blocks = new List<(bool Compiles, List<bool> Verdicts)>();
        foreach (var raw in result.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("Regexp: ", StringComparison.Ordinal))
            {
                blocks.Add((true, []));
            }
            else if (line.Trim() == "failed to compile")
            {
                blocks[^1] = (false, blocks[^1].Verdicts);
            }
            else if (line.EndsWith(": Ok", StringComparison.Ordinal))
            {
                blocks[^1].Verdicts.Add(true);
            }
            else if (line.EndsWith(": Fail", StringComparison.Ordinal))
            {
                blocks[^1].Verdicts.Add(false);
            }
        }

        return blocks;
    }
}
