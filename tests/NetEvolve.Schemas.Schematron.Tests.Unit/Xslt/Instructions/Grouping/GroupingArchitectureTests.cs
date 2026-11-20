namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

/// <summary>
/// The Grouping folder reaches patterns only through the shared matcher (SCH-54, #212; design #193) and holds no pattern code of its
/// own, writes no LINQ, and uses neither System.Xml nor System.Text.Json (#254).
/// </summary>
public class GroupingArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping";

    // The vocabulary of a pattern matcher: the plan, the steps and their tests, and the profile of the subset.
    private static readonly string[] PatternCodeNames =
    [
        "PatternStep",
        "PatternAlternative",
        "PatternPlan",
        "PatternBuilder",
        "PatternProfile",
        "PatternPredicates",
        "PatternPriority",
        "PatternLocator",
        "AxisStep",
        "NameTest",
        "NodeTest",
        "XPathNodeTest",
        "XPathSubsetChecker",
        "Axis.",
        "XPathParser",
        "PathExpression",
        "PathRoot",
        "FunctionCall",
        "SetExpression",
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
            "Grouping"
        );
        var sources = new List<(string, string)>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            sources.Add((Path.GetFileName(file), await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return sources;
    }

    [Test]
    public async Task Grouping_CompilesThePatternWithTheSharedMatcher_AndMatchesWithIt(
        CancellationToken cancellationToken
    )
    {
        var sources = await SourcesAsync(cancellationToken);
        var instruction = sources.Find(source =>
            string.Equals(source.File, "XsltForEachGroupInstruction.cs", StringComparison.Ordinal)
        );
        var grouping = sources.Find(source =>
            string.Equals(source.File, "XsltPatternGrouping.cs", StringComparison.Ordinal)
        );

        await Assert.That(instruction.Text).Contains("new PatternMatcher(");
        await Assert.That(instruction.Text).Contains("PatternUse.");
        await Assert.That(grouping.Text).Contains("_matcher.Matches(");
    }

    [Test]
    public async Task Grouping_ContainNoPatternCodeOfTheirOwn(CancellationToken cancellationToken)
    {
        var sources = await SourcesAsync(cancellationToken);
        var offenders = new List<string>();
        foreach (var (file, text) in sources)
        {
            // A name in a documentation comment is no code: look at the lines that are not comments.
            foreach (var line in text.Split('\n'))
            {
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var name in PatternCodeNames)
                {
                    if (
                        Regex.IsMatch(
                            code,
                            @"\b" + Regex.Escape(name),
                            RegexOptions.CultureInvariant,
                            TimeSpan.FromSeconds(1)
                        )
                    )
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
    public async Task Grouping_ContainNoLinq_NoSystemXml_AndNoHandWrittenArgumentThrow(
        CancellationToken cancellationToken
    )
    {
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
                || text.Contains("ArgumentNullException.ThrowIfNull", StringComparison.Ordinal)
            )
            {
                offenders.Add(file);
            }
        }

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Grouping_ImplementNoSecondPatternMatcher_BecauseTheSharedMatcherIsTheOneOfDesign137()
    {
        var checkedTypes = 0;
        var matchers = new List<string>();
        foreach (var type in typeof(XsltForEachGroupInstruction).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                continue;
            }

            checkedTypes++;
            if (typeof(IPatternMatcher).IsAssignableFrom(type))
            {
                matchers.Add(type.FullName!);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(4);
        await Assert.That(matchers).IsEmpty();
    }

    [Test]
    public async Task Grouping_HoldsTheTwoFunctions_AndNoSortInstructionOfItsOwn()
    {
        var names = new List<string>();
        foreach (var type in typeof(XsltForEachGroupInstruction).Assembly.GetTypes())
        {
            if (string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                names.Add(type.Name);
            }
        }

        await Assert.That(names).Contains("CurrentGroupFunction");
        await Assert.That(names).Contains("CurrentGroupingKeyFunction");
        await Assert.That(names).DoesNotContain("XsltSortInstruction");
    }
}
