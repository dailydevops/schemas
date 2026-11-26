namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Characterization of the reader for the constructs that the tokenizer may return as one coarse token (an attribute with a
/// literal value, a start tag without attributes, an end tag without white space, #1151): the events, the element positions,
/// and the diagnostics of the unsplit input equal those of the input cut at every character, where no construct is complete.
/// </summary>
public class XmlParserCoarseTokenTests
{
    public static IEnumerable<Func<string>> Documents()
    {
        yield return () => "<a>x</a>";
        yield return () => "<a b=\"1\" c='2'>x</a>";
        yield return () => "<a b=\"\" c=''/>";
        yield return () => "<a b = \"1\"\tc\r\n=\r\n'2' >x</a >";
        yield return () => "<a b=\"x&amp;y\" c='&#65;&lt;' d=\"t\tu\r\nv\">x</a>";
        yield return () => "<a b='say \"hi\"' c=\"it's\">x</a>";
        yield return () => "<p:a xmlns:p=\"urn:p\" p:b=\"1\" xmlns=\"urn:d\"><p:c/><d/></p:a>";
        yield return () => "<a\r\n  b=\"1\"\r\n  c=\"2\">\r\n<b>t</b>\r\n</a>\r\n";
        yield return () => "<a b=\"1\" b=\"2\"/>";
        yield return () => "<a b=\"1\" xmlns:p=\"u\" xmlns:p=\"v\"/>";
        yield return () => "<a><b></c></a>";
        yield return () => "<a><b></b ></a>";
        yield return () => "<a></a><b/>";
        yield return () => "<a b=\"1\u0001\"/>";
        yield return () => "<a b=\"1<2\"/>";
        yield return () => "<a b=\"\U0001F600\U0001F600\">\U0001F600</a>";
        yield return () => "<a b=\"\uD83D\"/>";
        yield return () => "<a b=\"1\"c=\"2\"/>";
        yield return () => "<a b=\"1\"";
        yield return () => "<a b=\"1";
        yield return () => "<a b=";
        yield return () => "<a>";
        yield return () => "<a></a";
        yield return () => "﻿<a b=\"1\">é</a>";
        yield return () => "<?xml version=\"1.0\"?><a b=\"1\"><c>d</c></a>";
        yield return () => "<!--c--><a><b c=\"1\"><d/></b></a><!--e-->";
        yield return () => "<a b=\"1\" <c/>";
        yield return () => "<1a/>";
        yield return () => "<a><b>x</b></a>trailing";
    }

    public static IEnumerable<Func<(string Xml, string Limit)>> LimitedDocuments()
    {
        foreach (var factory in Documents())
        {
            var xml = factory();
            foreach (var limit in new[] { "document", "tag", "value", "name", "depth", "attributes", "nodes" })
            {
                yield return () => (xml, limit);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Documents))]
    public async Task Run_AnyChunking_GivesTheEventsPositionsAndDiagnosticOfUnsplitInput(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = Run(xml, -1, 0, XmlReaderLimits.Default);
        var divergent = new List<string>();
        foreach (var size in new[] { 1, 2, 3, 5, 8 })
        {
            if (Run(xml, -1, size, XmlReaderLimits.Default) != expected)
            {
                divergent.Add($"chunks of {size}");
            }
        }

        for (var split = 1; split < xml.Length; split++)
        {
            if (Run(xml, split, 0, XmlReaderLimits.Default) != expected)
            {
                divergent.Add($"split at {split}");
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    // Every limit value up to one past the document length: each character is the one that crosses a limit once.
    [Test]
    [MethodDataSource(nameof(LimitedDocuments))]
    public async Task Run_LimitAtAnyCharacter_GivesTheSameResultForUnsplitAndSingleCharacterInput(
        string xml,
        string limit
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(limit);
        var divergent = new List<string>();
        for (var value = 1; value <= xml.Length + 1 && divergent.Count < 5; value++)
        {
            var limits = With(limit, value);
            if (Run(xml, -1, 0, limits) != Run(xml, -1, 1, limits))
            {
                divergent.Add($"{limit} {value}");
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    [Test]
    public async Task Run_CompleteConstructs_GiveTheEventsOfFineGrainedTokens()
    {
        var run = Run("<a b=\"1\" c='2'><d>x</d></a>", -1, 0, XmlReaderLimits.Default);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(run.Events).StartsWith("SD()|<a|@b=1|@c=2|>|<d|>|T'x'|/d|/a|");
        await Assert.That(run.Positions).IsEqualTo("1:1 (offset 0);1:16 (offset 15);");
    }

    [Test]
    public async Task Run_AttributeAtTheValueLengthLimit_IsAcceptedAndOneMoreCharacterIsAnError()
    {
        var limits = XmlReaderLimits.Default.WithMaxAttributeValueLength(4);

        var fits = Run("<a b=\"abcd\"/>", -1, 0, limits);
        var crosses = Run("<a b=\"abcde\"/>", -1, 0, limits);

        await Assert.That(fits.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(crosses.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(crosses.Code?.Alias).IsEqualTo("XML1112");
        await Assert.That(crosses.ErrorPosition?.CharOffset).IsEqualTo(10L);
    }

    [Test]
    public async Task Run_StartTagAtTheStartTagLimit_IsAcceptedAndOneMoreCharacterIsAnError()
    {
        var fits = Run("<abc>x</abc>", -1, 0, XmlReaderLimits.Default.WithMaxStartTagCharacters(5));
        var crosses = Run("<abc>x</abc>", -1, 0, XmlReaderLimits.Default.WithMaxStartTagCharacters(4));

        await Assert.That(fits.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(crosses.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(crosses.Code?.Alias).IsEqualTo("XML1113");
        await Assert.That(crosses.ErrorPosition?.CharOffset).IsEqualTo(4L);
    }

    [Test]
    public async Task Run_EndTagAtTheDocumentLimit_IsAcceptedAndOneMoreCharacterIsAnError()
    {
        var fits = Run("<a>x</a>", -1, 0, XmlReaderLimits.Default.WithMaxDocumentCharacters(8));
        var crosses = Run("<a>x</a>", -1, 0, XmlReaderLimits.Default.WithMaxDocumentCharacters(7));

        await Assert.That(fits.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(crosses.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(crosses.Code?.Alias).IsEqualTo("XML1101");
        await Assert.That(crosses.ErrorPosition?.CharOffset).IsEqualTo(7L);
    }

    [Test]
    [Arguments("<a b=\"1\" b=\"2\"/>", "XML0006", 9L)]
    [Arguments("<a><b></c></a>", "XML0005", 6L)]
    [Arguments("<a b=\"1\"c=\"2\"/>", "XML0011", 8L)]
    public async Task Run_ErrorInACoarseConstruct_IsAtTheStartOfThatConstruct(string xml, string code, long offset)
    {
        var run = Run(xml, -1, 0, XmlReaderLimits.Default);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code?.Alias).IsEqualTo(code);
        await Assert.That(run.ErrorPosition?.CharOffset).IsEqualTo(offset);
    }

    private static XmlReaderLimits With(string limit, int value) =>
        limit switch
        {
            "document" => XmlReaderLimits.Default.WithMaxDocumentCharacters(value),
            "tag" => XmlReaderLimits.Default.WithMaxStartTagCharacters(value),
            "value" => XmlReaderLimits.Default.WithMaxAttributeValueLength(value),
            "name" => XmlReaderLimits.Default.WithMaxNameLength(value),
            "depth" => XmlReaderLimits.Default.WithMaxElementDepth(value),
            "attributes" => XmlReaderLimits.Default.WithMaxAttributesPerElement(value),
            _ => XmlReaderLimits.Default.WithMaxNodeCount(value),
        };

    // The reader position after an error follows token boundaries and is not part of the contract.
    private static ReaderRun Run(string xml, int split, int size, XmlReaderLimits limits)
    {
        var run = ReaderHarness.Run(xml, limits, split, size);

        return run.Status == XmlReadStatus.Error ? run with { Position = default } : run;
    }
}
