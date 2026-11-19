namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Reader.EntityInput;

/// <summary>
/// A resource limit error gives the events, the error code, and the position of the unsplit input, however the input is cut into
/// chunks (#655). The reference is the character that crosses the limit: the error is at that character, and the characters
/// before it are reported first. The reader position after an error is not part of the contract.
/// </summary>
public class XmlParserLimitChunkInvarianceTests
{
    private const string DoctypeDocument =
        "<!DOCTYPE a [<!ENTITY e \"x&amp;<b/>\"><!ENTITY v \"12\"><!ELEMENT a (#PCDATA)>]>"
        + "<a x=\"&v;\">&e;<![CDATA[z]]></a>";

    private static readonly int[] ChunkSizes = [1, 2, 3, 5];

    private static readonly string[] Limits =
    [
        "document",
        "text",
        "value",
        "tag",
        "depth",
        "attributes",
        "namespaces",
        "name",
        "token",
        "nodes",
        "declaration",
        "doctype",
    ];

    public static IEnumerable<Func<(string Xml, bool Doctype)>> Documents()
    {
        yield return () => ("<a>ab<![CDATA[cd]]>ef&amp;gh</a>", false);
        yield return () => ("<a><![CDATA[x]]><b c=\"1\"/></a>", false);
        yield return () =>
            ("<?xml version=\"1.0\"?>\r\n<!--c--><r a=\"xy&amp;z\" b='w'>t\r\nu<e k=\"1\"/>x</r>\n", false);
        yield return () => ("<a x=\"12345&#65;&lt;678\" y='ab'>z</a>", false);
        yield return () => ("﻿<a b=\"1\">x</a>", false);
        yield return () => ("<a   b = \"1\"   c='2' >x</a >", false);
        yield return () => ("<a b=\"\U0001F600\U0001F600\">\U0001F600\U0001F600\U0001F600</a>", false);
        yield return () => ("<a>ab\u0001cd</a>", false);
        yield return () => ("<a x=\"ab\u0001cd\"/>", false);
        yield return () => ("<a><!--abc\u0001--></a>", false);
        yield return () => ("<a><!--abc--><?p data?>tail</a>", false);
        yield return () => ("<a>abc", false);
        yield return () => ("<a x=\"abc", false);
        yield return () => ("<a x", false);
        yield return () => ("<a/>\n \n<!--c-->", false);
        yield return () => (DoctypeDocument, true);
        yield return () => ("<a xmlns:p=\"u\" xmlns=\"v\" x=\"1\" y=\"2\"><b><c/><?p d?><!--c--></b></a>", false);
    }

    public static IEnumerable<Func<(string Xml, bool Doctype, string Limit)>> LimitedDocuments()
    {
        foreach (var factory in Documents())
        {
            var (xml, doctype) = factory();
            foreach (var limit in Limits)
            {
                var name = limit;
                yield return () => (xml, doctype, name);
            }
        }
    }

    // Every value from the smallest one up to one past the length, so each character of the document is the one that crosses.
    [Test]
    [MethodDataSource(nameof(LimitedDocuments))]
    public async Task Pump_LimitInAnyChunking_GivesTheEventsCodeAndPositionOfUnsplitInput(
        string xml,
        bool doctype,
        string limit
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(limit);
        var divergent = new List<string>();
        for (var value = 1; value <= xml.Length + 1 && divergent.Count < 5; value++)
        {
            var limits = With(limit, value);
            var expected = Run(xml, -1, 0, doctype, limits);
            for (var split = 1; split < xml.Length; split++)
            {
                if (Run(xml, split, 0, doctype, limits) != expected)
                {
                    divergent.Add($"{limit} {value} split at {split}");
                }
            }

            foreach (var size in ChunkSizes)
            {
                if (Run(xml, -1, size, doctype, limits) != expected)
                {
                    divergent.Add($"{limit} {value} chunks of {size}");
                }
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    // Two limits at once: the one that is crossed at the earlier character is reported, and a tie goes to the document limit.
    [Test]
    [Arguments("<a>ab<![CDATA[cd]]>ef</a>")]
    [Arguments("<a x=\"abcdef\">abcdef</a>")]
    [Arguments("<a x=\"abc&amp;def\">ab&#65;c&amp;<![CDATA[def]]>gh</a>")]
    public async Task Pump_TwoLimitsInAnyChunking_GiveTheEventsCodeAndPositionOfUnsplitInput(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var divergent = new List<string>();
        for (var document = 1; document <= xml.Length + 1; document += 2)
        {
            for (var text = 1; text <= 8; text++)
            {
                for (var tag = 1; tag <= 12; tag += 2)
                {
                    var limits = XmlReaderLimits
                        .Default.WithMaxDocumentCharacters(document)
                        .WithMaxTextNodeLength(text)
                        .WithMaxAttributeValueLength(text)
                        .WithMaxStartTagCharacters(tag);
                    var expected = Run(xml, -1, 0, false, limits);
                    foreach (var size in ChunkSizes)
                    {
                        if (Run(xml, -1, size, false, limits) != expected)
                        {
                            divergent.Add($"{xml} {document}/{text}/{tag} chunks of {size}");
                        }
                    }
                }
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    // The character that crosses the limit is the error, the characters before it are events.
    [Test]
    [Arguments("<a>ab<![CDATA[cd]]>ef</a>", "text", 4, "XML1107", 19, "SD()|<a|>|T'ab'|CD'cd'")]
    [Arguments("<a>abcdef</a>", "text", 3, "XML1107", 6, "SD()|<a|>|T'abc'")]
    [Arguments("<a x=\"abcde\"/>", "value", 3, "XML1112", 9, "SD()")]
    [Arguments("<a x=\"abcde\"/>", "tag", 5, "XML1113", 5, "SD()")]
    [Arguments("<a x=\"1\"   />", "tag", 9, "XML1113", 9, "SD()")]
    [Arguments("<a>abcdef</a>", "document", 6, "XML1101", 6, "SD()|<a|>|T'abc'")]
    [Arguments("<a>ab<![CDATA[cd]]>ef</a>", "document", 16, "XML1101", 16, "SD()|<a|>|T'ab'|CD'cd'")]
    [Arguments("<a><!--abcdef--></a>", "document", 6, "XML1101", 6, "SD()|<a|>")]
    [Arguments("<a>\U0001F600\U0001F600</a>", "text", 3, "XML1107", 5, "SD()|<a|>|T'\U0001F600'")]
    [Arguments("<a>\U0001F600\U0001F600</a>", "document", 6, "XML1101", 5, "SD()|<a|>|T'\U0001F600'")]
    [Arguments("<a>abc\u0001d</a>", "text", 3, "XML0002", 6, "SD()|<a|>|T'abc'")]
    [Arguments("<a>abc\u0001d</a>", "document", 6, "XML0002", 6, "SD()|<a|>|T'abc'")]
    public async Task Pump_LimitCrossedAtACharacter_ReportsThatCharacterAfterTheCharactersBeforeIt(
        string xml,
        string limit,
        int value,
        string code,
        int offset,
        string events
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        var limits = With(limit, value);
        foreach (var size in new[] { 0, 1, 2 })
        {
            var run = Run(xml, -1, size, false, limits);

            await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
            await Assert.That(run.Code?.Alias).IsEqualTo(code);
            await Assert.That(run.ErrorPosition?.CharOffset).IsEqualTo((long)offset);
            await Assert.That(run.Events).IsEqualTo(events);
        }
    }

    // Inside an attribute value the limit and the invalid character are ordered by position: the one that comes first is reported,
    // and the invalid character wins a tie, as in text. This holds however the input is cut.
    [Test]
    [Arguments("<a x=\"abcd\"/>", "value", 2, "XML1112", 8)]
    [Arguments("<a x=\"abcd\"/>", "value", 3, "XML0002", 9)]
    [Arguments("<a x=\"abcd\"/>", "value", 5, "XML0002", 9)]
    [Arguments("<a x=\"abcd\"/>", "tag", 8, "XML1113", 8)]
    [Arguments("<a x=\"abcd\"/>", "tag", 10, "XML0002", 9)]
    [Arguments("<a x=\"abcd\"/>", "document", 8, "XML1101", 8)]
    [Arguments("<a x=\"abcd\"/>", "document", 10, "XML0002", 9)]
    [Arguments("<a b=\"\"/>", "document", 4, "XML1101", 4)]
    [Arguments("<a b=\"\"/>", "tag", 4, "XML1113", 4)]
    [Arguments("<a b=\"\"/>", "document", 6, "XML0002", 6)]
    [Arguments("<a b=''/>", "document", 4, "XML1101", 4)]
    [Arguments("<a b=/>", "document", 4, "XML1101", 4)]
    public async Task Pump_LimitAndInvalidCharacterInAnAttributeValue_ReportTheOneThatComesFirst(
        string xml,
        string limit,
        int value,
        string code,
        int offset
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(limit);
        var limits = With(limit, value);
        foreach (var size in new[] { 0, 1, 2, 3, 5 })
        {
            var run = Run(xml, -1, size, false, limits);

            await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error).Because($"chunks of {size}");
            await Assert.That(run.Code?.Alias).IsEqualTo(code).Because($"chunks of {size}");
            await Assert.That(run.ErrorPosition?.CharOffset).IsEqualTo((long)offset).Because($"chunks of {size}");
            await Assert.That(run.Events).IsEqualTo("SD()").Because($"chunks of {size}");
        }
    }

    // The start tag limit counts from the opening bracket: neither a byte order mark nor the "]]>" that ends a CDATA section before
    // the tag is part of it, however the input is cut.
    [Test]
    [Arguments("<a><![CDATA[x]]><b c=\"1\"/></a>", 10, 25)]
    [Arguments("﻿<a b=\"1\">x</a>", 9, 8)]
    public async Task Pump_StartTagLimit_CountsFromTheOpeningBracket(string xml, int fits, int offset)
    {
        ArgumentNullException.ThrowIfNull(xml);
        foreach (var size in new[] { 0, 1, 2, 3 })
        {
            var passes = Run(xml, -1, size, false, With("tag", fits));
            var fails = Run(xml, -1, size, false, With("tag", fits - 1));

            await Assert.That(passes.Status).IsEqualTo(XmlReadStatus.Completed);
            await Assert.That(fails.Code?.Alias).IsEqualTo("XML1113");
            await Assert.That(fails.ErrorPosition?.CharOffset).IsEqualTo((long)offset);
        }
    }

    private static XmlReaderLimits With(string limit, int value) =>
        limit switch
        {
            "document" => XmlReaderLimits.Default.WithMaxDocumentCharacters(value),
            "text" => XmlReaderLimits.Default.WithMaxTextNodeLength(value),
            "value" => XmlReaderLimits.Default.WithMaxAttributeValueLength(value),
            "tag" => XmlReaderLimits.Default.WithMaxStartTagCharacters(value),
            "depth" => XmlReaderLimits.Default.WithMaxElementDepth(value),
            "attributes" => XmlReaderLimits.Default.WithMaxAttributesPerElement(value),
            "namespaces" => XmlReaderLimits.Default.WithMaxNamespaceDeclarationsPerElement(value),
            "name" => XmlReaderLimits.Default.WithMaxNameLength(value),
            "token" => XmlReaderLimits.Default.WithMaxTokenLength(value),
            "nodes" => XmlReaderLimits.Default.WithMaxNodeCount(value),
            "declaration" => XmlReaderLimits.Default.WithMaxXmlDeclarationLength(value),
            _ => XmlReaderLimits.Default.WithMaxDoctypeLength(value),
        };

    // One parse, cut once at `split` or evenly into chunks of `size`. The position after an error is not compared.
    private static ReaderRun Run(string xml, int split, int size, bool doctype, XmlReaderLimits limits)
    {
        var handler = doctype ? new DtdDoctypeHandler(limits) : null;
        var stack = handler is null ? null : new XmlEntityInputStack(limits, handler);
        var run = ReaderHarness.Run(xml, limits, split, size, handler, stack);

        return run.Status == XmlReadStatus.Error ? run with { Position = default } : run;
    }
}
