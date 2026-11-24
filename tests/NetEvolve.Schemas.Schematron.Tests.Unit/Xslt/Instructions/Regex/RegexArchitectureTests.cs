namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

/// <summary>
/// The Regex folder walks an input only through the match-segment iterator of the XPath package (XP-24, #266): it holds no regex
/// traversal of its own and does not use the regex engine of the BCL, writes no LINQ, and uses neither System.Xml nor
/// System.Text.Json (#254).
/// </summary>
public class RegexArchitectureTests
{
    // The ways to walk a regex over a text other than the iterator: the match enumerator and operations of the Xml package, and the BCL.
    private static readonly string[] OwnTraversal =
    [
        "RegexMatchEnumerator",
        "EnumerateMatches",
        "System.Text.RegularExpressions",
        ".TryMatch(",
        ".IsMatch(",
        "RegexParser",
        "RegexOptionsParser",
    ];

    private static async Task<List<(string File, string Text)>> SourcesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = Path.Combine(
            SuitePinFiles.RepositoryRoot,
            "src",
            "NetEvolve.Schemas.Schematron",
            "Xslt",
            "Instructions",
            "Regex"
        );
        var sources = new List<(string, string)>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            sources.Add((Path.GetFileName(file), await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return sources;
    }

    [Test]
    public async Task Regex_WalksTheInputWithTheIteratorOfTheXPathPackage(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var executable = sources.Find(source =>
            string.Equals(source.File, "XsltAnalyzeStringExecutable.cs", StringComparison.Ordinal)
        );

        await Assert.That(executable.Text).Contains("new MatchSegmentEnumerator(");
        await Assert.That(executable.Text).Contains("segments.MoveNext()");
        await Assert.That(executable.Text).Contains("PatternCache.Shared.Get(");
    }

    [Test]
    public async Task Regex_ContainsNoRegexTraversalOfItsOwn(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var offenders = new List<string>();
        foreach (var (file, text) in sources)
        {
            // A name in a documentation comment is no code: look at the lines that are not comments.
            foreach (var line in text.Split('\n'))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var name in OwnTraversal)
                {
                    if (line.Contains(name, StringComparison.Ordinal))
                    {
                        offenders.Add($"{file}: {name}");
                    }
                }
            }
        }

        await Assert.That(sources.Count).IsGreaterThan(4);
        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Regex_ContainsNoLinq_NoSystemXml_AndNoHandWrittenArgumentThrow(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var offenders = new List<string>();
        foreach (var (file, text) in await SourcesAsync(cancellationToken))
        {
            if (
                text.Contains("System.Linq", StringComparison.Ordinal)
                || text.Contains(".Select(", StringComparison.Ordinal)
                || text.Contains(".Where(", StringComparison.Ordinal)
                || text.Contains("using System.Xml", StringComparison.Ordinal)
                || text.Contains("System.Text.Json", StringComparison.Ordinal)
                || text.Contains("throw new Argument", StringComparison.Ordinal)
            )
            {
                offenders.Add(file);
            }
        }

        await Assert.That(offenders).IsEmpty();
    }
}
