namespace NetEvolve.Schemas.XPath.Tests.Unit.Readme;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.XPath/README.md, character for character.
public class XPathReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.XPath/README.md";
    private const string Template = "templates/readme-project.md";
    private const string Samples = "tests/NetEvolve.Schemas.XPath.Tests.Unit/Readme/XPathReadmeTests.cs";

    // <sample>
    internal sealed class DoubleFunction() : XPathHostFunction(new XPathQName("urn:example:app", "double"), 1, 1)
    {
        public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
        {
            var argument = invocation.Arguments[0];
            if (argument.Count != 1 || !argument[0].TryGetInteger(out var value))
            {
                invocation.Diagnostic = XPathDiagnostic.Create(new XPathErrorCode("urn:example:app", "APP0001"));
                return XPathEvaluationStatus.Error;
            }

            invocation.Result = XdmSequence.Create(XdmItem.FromInteger(value * 2));
            return XPathEvaluationStatus.Ok;
        }
    }

    // </sample>

    [Test]
    public async Task QuickStartSample_SumsTheAttributes()
    {
        // <sample>
        var order = new XPathNode(
            XmlDocumentLoader.Load("""<order><line price="10"/><line price="15"/></order>""".AsSpan()).Document
        );
        var expression = XPathExpression.Compile("sum(/order/line/@price)");

        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(order));
        var result = expression.Evaluate(context);
        // </sample>

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetDouble(out var sum)).IsTrue();
        await Assert.That(sum).IsEqualTo(25d);
    }

    [Test]
    public async Task TryCompileSample_ReportsTheSyntaxErrorWithoutThrowing()
    {
        // <sample>
        var compiled = XPathExpression.TryCompile("1 +");
        // </sample>

        await Assert.That(compiled.Success).IsFalse();
        await Assert.That(compiled.Expression).IsNull();
        await Assert.That(compiled.Diagnostic.Code).IsEqualTo("XPST0003");
        await Assert.That(compiled.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Static);
    }

    [Test]
    public async Task VariableSample_ComparesAgainstTheBoundValue()
    {
        // <sample>
        var order = new XPathNode(
            XmlDocumentLoader.Load("""<order><line price="10"/><line price="15"/></order>""".AsSpan()).Document
        );
        var limit = new XPathQName(string.Empty, "limit");
        var statics = XPathStaticContext.Default.ToBuilder().WithVariable(limit).Build();
        var expression = XPathExpression.Compile("count(/order/line[@price > $limit])", statics);

        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(order));
        context.Variables.Set(limit, XdmItem.FromInteger(12));
        var result = expression.TryEvaluate(context);
        // </sample>

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(result.Value[0].TryGetInteger(out var count)).IsTrue();
        await Assert.That(count).IsEqualTo(1L);
    }

    [Test]
    public async Task HostFunctionSample_CallsTheRegisteredFunction()
    {
        // <sample>
        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("app", "urn:example:app")
            .WithFunction(new DoubleFunction())
            .Build();
        var expression = XPathExpression.Compile("app:double(21)", statics);

        using var context = new XPathEvaluationContext();
        var result = expression.TryEvaluate(context);
        // </sample>

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok);
        await Assert.That(result.Value[0].TryGetInteger(out var doubled)).IsTrue();
        await Assert.That(doubled).IsEqualTo(42L);

        using var other = new XPathEvaluationContext();
        var failing = XPathExpression.Compile("app:double('x')", statics).TryEvaluate(other);

        await Assert.That(failing.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(failing.Diagnostic.Code).IsEqualTo("{urn:example:app}APP0001");
    }

    [Test]
    public async Task CompatibilitySample_ConvertsAStringOperandToANumber()
    {
        // <sample>
        var compatible = XPathStaticContext.Default.ToBuilder().WithXPath10Compatibility(true).Build();
        var expression = XPathExpression.Compile("'3' + 4", compatible);

        using var context = new XPathEvaluationContext();
        var result = expression.Evaluate(context);
        // </sample>

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetDouble(out var sum)).IsTrue();
        await Assert.That(sum).IsEqualTo(7d);

        using var strict = new XPathEvaluationContext();
        var outcome = XPathExpression.Compile("'3' + 4").TryEvaluate(strict);

        await Assert.That(outcome.Diagnostic.Code).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task SecuritySample_DeniesExternalDocumentsWithoutAResolver()
    {
        // <sample>
        var expression = XPathExpression.Compile("doc('https://example.com/orders.xml')");

        using var context = new XPathEvaluationContext();
        var result = expression.TryEvaluate(context);
        // </sample>

        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(result.Diagnostic.Code).IsEqualTo("FODC0002");
        await Assert.That(context.ResourceResolver).IsNull();
    }

    [Test]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task LaterVersions_AreNotEnabledByTheCompileEntry(XPathVersion version)
    {
        var statics = XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

        var compiled = XPathExpression.TryCompile("1", statics);

        await Assert.That(compiled.Success).IsFalse();
        await Assert.That(compiled.Diagnostic.Code).IsEqualTo("{urn:netevolve:schemas:xpath}XP0001");
    }

    [Test]
    [Arguments("math:pi()")]
    [Arguments("fn:format-integer(1, '1')")]
    [Arguments("fn:analyze-string('a', 'a')")]
    public async Task LaterVersionFunctions_AreNotCallableThroughTheVersion20Entry(string expression)
    {
        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .WithNamespace("math", "http://www.w3.org/2005/xpath-functions/math")
            .Build();

        var compiled = XPathExpression.TryCompile(expression, statics);

        await Assert.That(compiled.Success).IsFalse();
        await Assert.That(compiled.Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task EvaluationLimits_DocumentedDefaultsEqualTheDefaultsInCode()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var limits = XPathLimits.Default;
        var core = ValidationLimits.Default;

        await Assert.That(Cell(readme, "Recursion depth")).IsEqualTo(Format(limits.MaxRecursionDepth));
        await Assert.That(Cell(readme, "Sequence size")).IsEqualTo(Format(limits.MaxSequenceSize));
        await Assert.That(Cell(readme, "Range size")).IsEqualTo(Format(limits.MaxRangeSize));
        await Assert.That(Cell(readme, "String length")).IsEqualTo(Format(limits.MaxStringLength));
        await Assert.That(Cell(readme, "External resources")).IsEqualTo(Format(core.MaxExternalResources));
        await Assert
            .That(Cell(readme, "Resource size"))
            .IsEqualTo(Format(core.MaxResourceSize / (1024 * 1024)) + " MiB");
    }

    [Test]
    public async Task Conformance_StatedBaselineEntriesEqualTheCommittedBaselines()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains(Format(CountEntries("qt3")) + " baseline entries");
        await Assert.That(readme).Contains(Format(CountEntries("qt3-xp30")) + " baseline entries");
        await Assert.That(readme).Contains(Format(CountEntries("qt3-xp31")) + " baseline entries");
    }

    [Test]
    public async Task Conformance_NamesTheSuiteCommitOfTheManifest()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var manifest = ReadmeSamples.ReadText("tests/conformance/suites/qt3.json");
        var start = manifest.IndexOf("qt3tests-", StringComparison.Ordinal) + "qt3tests-".Length;
        var commit = manifest.Substring(start, 40);

        await Assert.That(readme).Contains(commit);
    }

    [Test]
    [Arguments("DEF-40", 52)]
    [Arguments("DEF-41", 53)]
    [Arguments("DEF-42", 54)]
    [Arguments("DEF-43", 55)]
    [Arguments("DEF-49", 61)]
    [Arguments("DEF-75", 87)]
    public async Task Exclusion_NamesItsDefIdAndLinksTheIssue(string id, int issue)
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var link =
            "(https://github.com/dailydevops/schemas/issues/" + issue.ToString(CultureInfo.InvariantCulture) + ")";

        var found = false;
        foreach (var line in readme.Split('\n'))
        {
            found |= line.Contains(id, StringComparison.Ordinal) && line.Contains(link, StringComparison.Ordinal);
        }

        await Assert.That(found).IsTrue().Because(id);
    }

    [Test]
    public async Task Readme_FollowsTheSectionOrderOfTheProjectTemplate()
    {
        var template = Headings(ReadmeSamples.ReadText(Template));
        var readme = Headings(ReadmeSamples.ReadText(Readme));
        var position = 0;

        foreach (var heading in template)
        {
            if (heading.StartsWith("# Project Name", StringComparison.Ordinal))
            {
                continue;
            }

            var found = readme.IndexOf(heading, position);
            await Assert.That(found).IsGreaterThanOrEqualTo(0).Because(heading);
            position = found + 1;
        }

        await Assert.That(readme[0]).IsEqualTo("# NetEvolve.Schemas.XPath");
    }

    [Test]
    public async Task Readme_HasNoTemplatePlaceholdersAndNoComments()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).DoesNotContain("<!--");
        await Assert.That(readme).DoesNotContain("YourPackageName");
        await Assert.That(readme).DoesNotContain("YourRepo");
        await Assert.That(readme).DoesNotContain("x.x.x");
        await Assert.That(readme).Contains("https://www.nuget.org/packages/NetEvolve.Schemas.XPath/");
    }

    [Test]
    public async Task Readme_LinksToFilesThatExist()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        const string prefix = "https://github.com/dailydevops/schemas/blob/main/";
        var checkedLinks = 0;

        foreach (var line in readme.Split('\n'))
        {
            var from = 0;
            while (true)
            {
                var start = line.IndexOf(prefix, from, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                var end = line.IndexOfAny([')', ' ', '`', '>'], start);
                var path = line.Substring(start + prefix.Length, (end < 0 ? line.Length : end) - start - prefix.Length);
                await Assert.That(File.Exists(RepositoryPath(path))).IsTrue().Because(path);
                checkedLinks++;
                from = start + prefix.Length;
            }
        }

        await Assert.That(checkedLinks).IsGreaterThan(0);
    }

    [Test]
    public async Task EveryReadmeCodeBlock_IsACompiledSample()
    {
        var compiled = ReadmeSamples.SamplesIn(Samples);
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
    }

    private static string Format(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string RepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Schemas.slnx was not found."),
            relativePath
        );
    }

    private static int CountEntries(string baseline)
    {
        var count = 0;
        var directory = RepositoryPath("tests/conformance/baselines/" + baseline);
        foreach (var file in Directory.EnumerateFiles(directory, "*.txt"))
        {
            foreach (var line in File.ReadAllLines(file))
            {
                if (line.Trim().Length > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static List<string> Headings(string markdown)
    {
        var headings = new List<string>();
        var inFence = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
            }
            else if (
                !inFence
                && (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            )
            {
                headings.Add(line);
            }
        }

        return headings;
    }

    // The trimmed second cell of the first table row whose first cell is the given text.
    private static string Cell(string readme, string firstCell)
    {
        var prefix = "| " + firstCell + " |";
        foreach (var line in readme.Split('\n'))
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line.Trim().Trim('|').Split('|')[1].Trim();
            }
        }

        throw new InvalidOperationException("No table row for " + firstCell);
    }
}
