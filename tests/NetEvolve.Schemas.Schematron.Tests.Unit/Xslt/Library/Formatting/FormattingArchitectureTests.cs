namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Formatting;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;
using NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The Formatting folder reads <c>xsl:decimal-format</c> and hands the formats to the XPath package: it holds no picture parsing and
/// no number formatting of its own (#288, design #272: one picture formatter).
/// </summary>
public class FormattingArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Library.Formatting";

    // The vocabulary of the picture engine of the XPath package: the parser, the formatter, and the parts of a picture string.
    private static readonly string[] PictureCodeNames =
    [
        "NumberPictureParser",
        "NumberFormatter",
        "FormatNumberFunction",
        "FormatNumberErrors",
        "DecimalFormatName",
        "NumberPicture.",
        "SubPicture",
        "PictureParser",
        "ParsePicture",
        "IntegerPart",
        "FractionPart",
        "GroupingSize",
        "MinimumIntegerSize",
        "MaximumFractionSize",
        "TextBuilder",
    ];

    private static async Task<List<(string File, string Text)>> SourcesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = Path.Combine(
            SuitePinFiles.RepositoryRoot,
            "src",
            "NetEvolve.Schemas.Schematron",
            "Xslt",
            "Library",
            "Formatting"
        );
        var sources = new List<(string, string)>();
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            sources.Add((Path.GetFileName(file), await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return sources;
    }

    [Test]
    public async Task Formatting_ContainNoPictureParsing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sources = await SourcesAsync(cancellationToken);
        var offenders = new List<string>();
        foreach (var (file, text) in sources)
        {
            // A name in a comment is no code: look at the lines that are not comments.
            foreach (var line in text.Split('\n'))
            {
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("///", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var name in PictureCodeNames)
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

        await Assert.That(sources.Count).IsGreaterThanOrEqualTo(5);
        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    public async Task Formatting_ContainNoLinq(CancellationToken cancellationToken)
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
    public async Task Formatting_DefineNoXPathFunction_BecauseFormatNumberBelongsToTheXPathPackage()
    {
        var checkedTypes = 0;
        var functions = new List<string>();
        foreach (var type in typeof(XsltDecimalFormatTable).Assembly.GetTypes())
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

        await Assert.That(checkedTypes).IsGreaterThanOrEqualTo(5);
        await Assert.That(functions).IsEmpty();
    }

    [Test]
    public async Task Formatting_TableIsTheDecimalFormatSlotContractOfTheXPathPackage() =>
        await Assert
            .That(typeof(IXPathDecimalFormatProvider).IsAssignableFrom(typeof(XsltDecimalFormatTable)))
            .IsTrue();
}
