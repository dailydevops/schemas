namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;

/// <summary>The Keys folder reaches patterns only through the shared matcher and holds no pattern code of its own (#250, design #193).</summary>
public class KeysArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Keys";

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

        var folder = Path.Combine(SuitePinFiles.RepositoryRoot, "src", "NetEvolve.Schemas.Schematron", "Xslt", "Keys");
        var sources = new List<(string, string)>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            sources.Add((Path.GetFileName(file), await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return sources;
    }

    [Test]
    public async Task Keys_CompileTheMatchPatternWithTheSharedMatcherAndTheKeyUse(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var instruction = sources.Find(source =>
            string.Equals(source.File, "XsltKeyInstruction.cs", StringComparison.Ordinal)
        );

        await Assert.That(instruction.Text).Contains("new PatternMatcher(");
        await Assert.That(instruction.Text).Contains("PatternUse.KeyMatch");
    }

    [Test]
    public async Task Keys_ContainNoPatternCodeOfTheirOwn(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        await Assert.That(sources.Count).IsGreaterThan(5);
        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Keys_ContainNoLinq(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var offenders = new List<string>();
        foreach (var (file, text) in await SourcesAsync(cancellationToken))
        {
            if (
                text.Contains("System.Linq", StringComparison.Ordinal)
                || text.Contains(".Select(", StringComparison.Ordinal)
                || text.Contains(".Where(", StringComparison.Ordinal)
            )
            {
                offenders.Add(file);
            }
        }

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Keys_DefineNoXPathFunction_BecauseKeyBelongsToTheXPathPackage()
    {
        var checkedTypes = 0;
        var functions = new List<string>();
        foreach (var type in typeof(XsltKeyProvider).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                continue;
            }

            checkedTypes++;
            if (typeof(XPathHostFunction).IsAssignableFrom(type))
            {
                functions.Add(type.FullName!);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(5);
        await Assert.That(functions).IsEmpty();
    }

    [Test]
    public async Task Keys_ImplementNoSecondPatternMatcher_BecauseTheSharedMatcherIsTheOneOfDesign137()
    {
        var checkedTypes = 0;
        var matchers = new List<string>();
        foreach (var type in typeof(XsltKeyProvider).Assembly.GetTypes())
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

        await Assert.That(checkedTypes).IsGreaterThan(5);
        await Assert.That(matchers).IsEmpty();
    }

    [Test]
    public async Task Keys_ProviderImplementsTheKeysSlotContractOfTheXPathPackage() =>
        await Assert.That(typeof(IXPathKeyProvider).IsAssignableFrom(typeof(XsltKeyProvider))).IsTrue();
}
