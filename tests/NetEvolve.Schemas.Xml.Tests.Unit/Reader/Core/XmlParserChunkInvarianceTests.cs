namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Reader.EntityInput;

/// <summary>
/// Malformed input gives the events, the first error code, and the position of that error of the unsplit input, however the
/// input is cut into chunks (#598). The reader position after an error is the end of the last consumed token; it follows the
/// token boundaries and is therefore not part of the contract, while the position after a completed read is.
/// </summary>
public class XmlParserChunkInvarianceTests
{
    private const string DoctypeDocument =
        "<!DOCTYPE a [<!ENTITY e \"x&amp;<b/>\"><!ENTITY v \"1\"><!ELEMENT a (#PCDATA)>]>"
        + "<a x=\"&v;\">&e;<![CDATA[z]]></a>";

    private static readonly int[] ChunkSizes = [1, 2, 3, 5];

    // Characters that sit on a lexical edge: markers, quotes, line breaks, invalid characters, and half of a surrogate pair.
    private static readonly char[] Hazards =
    [
        '<',
        '>',
        '&',
        ';',
        '"',
        '\'',
        ']',
        '-',
        '?',
        '\r',
        '\n',
        ' ',
        '\t',
        '/',
        '=',
        '!',
        '\uD800',
        '\uDC00',
        '\u0001',
        '￾',
        'x',
        ':',
        '#',
        '[',
        '﻿',
    ];

    public static IEnumerable<Func<string>> MalformedDocuments()
    {
        // White space before the XML declaration: the error is at the declaration, and no event precedes it.
        yield return () => " <?xml version=\"1.0\"?><a/>";
        yield return () => "\n <?xml version=\"1.0\"?><a/>";
        yield return () => "\r\n<?xml version=\"1.0\"?><a/>";
        yield return () => " <?xml";

        // The characters before an invalid one are reported, the invalid one is the error.
        yield return () => "<a>ab\u0001cd</a>";
        yield return () => "<a><![CDATA[ab\u0001cd]]></a>";
        yield return () => "<a b=\"x\u0001y\"/>";
        yield return () => "<a>x\uD800y</a>";
        yield return () => "<a>\uDC00</a>";
        yield return () => "<a>a￾b</a>";

        // Text outside the root element: the error is at the first character that is not white space.
        yield return () => "<?xml version=\"1.0\"?>\r\n x<a/>";
        yield return () => "  \n  !-- c --><a/>";
        yield return () => "<a/> \n x";
        yield return () => "<a/>&amp;";

        yield return () => "<a>x]]>y</a>";
        yield return () => "<a><b></a>";
        yield return () => "<a b=\"1\" b=\"2\"/>";
    }

    public static IEnumerable<Func<(string Xml, bool Doctype)>> MutationBases()
    {
        yield return () =>
            (
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone='yes'?>\r\n<!-- c --><?pi d?>"
                    + "<r a=\"x&amp;y\" b='z'><![CDATA[a]]>t&lt;<e/></r>",
                false
            );
        yield return () => (" <?xml version=\"1.0\"?><a/>", false);
        yield return () => ("﻿<?xml version=\"1.0\"?>\n<a b=\"1\">x<!--c--><?p d?></a>", false);
        yield return () => ("<a>&#x41;&#10;&unknown;<b c='1' d=\"&lt;\"/>]]></a>", false);
        yield return () =>
            (
                "<?xml version='1.0'?><!--p--><?x y?><a:r xmlns:a='u'><a:s a:t='v'>\U0001F600x</a:s></a:r><!--e--><?z?>\n",
                false
            );
        yield return () => ("<\U0001F600 a\U0001F600='1'>x\tx</\U0001F600>", false);
        yield return () => ("<a:b xmlns:a=\"u\" a:c=\"v\">t</a:b>", false);
        yield return () => (DoctypeDocument, true);
    }

    [Test]
    [MethodDataSource(nameof(MalformedDocuments))]
    public async Task Pump_MalformedInputInAnyChunking_GivesTheEventsCodeAndPositionOfUnsplitInput(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = Run(xml, -1, 0, false);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Error);
        for (var split = 0; split <= xml.Length; split++)
        {
            await Assert.That(Run(xml, split, 0, false)).IsEqualTo(expected);
        }

        foreach (var size in ChunkSizes)
        {
            await Assert.That(Run(xml, -1, size, false)).IsEqualTo(expected);
        }
    }

    // A leading white-space run of up to the declaration limit is held until the declaration is known (XML0007 at the declaration);
    // a longer one is content and is not held, the same for every chunking. The runs sit on both sides of the limit.
    public static IEnumerable<Func<(string Xml, int Limit)>> LeadingWhiteSpaceRuns()
    {
        const int Limit = 3;
        foreach (var pattern in new[] { " ", "\n", " \n\t\r" })
        {
            for (var length = Limit - 1; length <= Limit + 2; length++)
            {
                var spaces = new string(' ', length).ToCharArray();
                for (var i = 0; i < length; i++)
                {
                    spaces[i] = pattern[i % pattern.Length];
                }

                var run = new string(spaces);
                yield return () => (run + "<?xml version=\"1.0\"?><a/>", Limit);
                yield return () => (run + "<a/>", Limit);
                yield return () => (run + "<?xml", Limit);
                yield return () => (run + "<?xml version=\"1.0\"?", Limit);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(LeadingWhiteSpaceRuns))]
    public async Task Pump_LeadingWhiteSpaceRunAroundTheDeclarationLimit_GivesTheResultOfUnsplitInput(
        string xml,
        int limit
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        var limits = XmlReaderLimits.Default.WithMaxXmlDeclarationLength(limit);
        var expected = Run(xml, -1, 0, false, limits);
        for (var split = 0; split <= xml.Length; split++)
        {
            await Assert.That(Run(xml, split, 0, false, limits)).IsEqualTo(expected);
        }

        foreach (var size in ChunkSizes)
        {
            await Assert.That(Run(xml, -1, size, false, limits)).IsEqualTo(expected);
        }
    }

    // The limit is 3: a run of up to 3 is held, so the declaration is the error (offset = run length, no event before it); a run of
    // 4 is content, so the document starts first and the declaration is then a processing instruction named "xml".
    [Test]
    [Arguments(2, false)]
    [Arguments(3, false)]
    [Arguments(4, true)]
    [Arguments(5, true)]
    public async Task Pump_LeadingWhiteSpaceRunBeforeTheDeclaration_IsContentOnlyBeyondTheLimit(
        int spaces,
        bool isContent
    )
    {
        var limits = XmlReaderLimits.Default.WithMaxXmlDeclarationLength(3);
        var run = Run(new string(' ', spaces) + "<?xml version=\"1.0\"?><a/>", -1, 0, false, limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code?.Alias).IsEqualTo("XML0007");
        await Assert.That(run.ErrorPosition?.CharOffset).IsEqualTo((long)spaces);
        await Assert.That(run.Events).IsEqualTo(isContent ? "SD()" : string.Empty);
    }

    [Test]
    [MethodDataSource(nameof(MutationBases))]
    public async Task Pump_MutatedDocumentsInAnyChunking_GiveTheEventsCodeAndPositionOfUnsplitInput(
        string xml,
        bool doctype
    )
    {
        ArgumentNullException.ThrowIfNull(xml);
        var divergent = new List<string>();
        foreach (var mutant in Mutants(xml))
        {
            var expected = Run(mutant, -1, 0, doctype);
            foreach (var size in ChunkSizes)
            {
                Compare(
                    divergent,
                    mutant,
                    FormattableString.Invariant($"chunks of {size}"),
                    expected,
                    Run(mutant, -1, size, doctype)
                );
            }

            for (var split = 1; split < mutant.Length; split++)
            {
                Compare(
                    divergent,
                    mutant,
                    FormattableString.Invariant($"split at {split}"),
                    expected,
                    Run(mutant, split, 0, doctype)
                );
            }

            if (divergent.Count > 5)
            {
                break;
            }
        }

        await Assert.That(divergent).IsEmpty();
    }

    // The unsplit input, every prefix, and every single-character deletion, insertion, and replacement with a hazard.
    private static IEnumerable<string> Mutants(string xml)
    {
        yield return xml;
        for (var i = 0; i < xml.Length; i++)
        {
            yield return xml.Substring(0, i);
            yield return xml.Remove(i, 1);
            foreach (var hazard in Hazards)
            {
                yield return xml.Insert(i, hazard.ToString());
                yield return xml.Remove(i, 1).Insert(i, hazard.ToString());
            }
        }
    }

    private static void Compare(List<string> divergent, string xml, string how, ReaderRun expected, ReaderRun actual)
    {
        if (actual != expected)
        {
            divergent.Add(
                FormattableString.Invariant(
                    $"{Visible(xml)} {how}: expected {expected.Status} {expected.Code?.Alias} at {expected.ErrorPosition} [{Visible(expected.Events)}], got {actual.Status} {actual.Code?.Alias} at {actual.ErrorPosition} [{Visible(actual.Events)}]"
                )
            );
        }
    }

    // Line breaks and lone surrogates are written out so a failure message is readable.
    private static string Visible(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            _ = c switch
            {
                '\r' => builder.Append("<CR>"),
                '\n' => builder.Append("<LF>"),
                _ when char.IsSurrogate(c) => builder.Append(CultureInfo.InvariantCulture, $"<U+{(int)c:X4}>"),
                _ when c < ' ' => builder.Append(CultureInfo.InvariantCulture, $"<U+{(int)c:X4}>"),
                _ => builder.Append(c),
            };
        }

        return builder.ToString();
    }

    // One parse, cut once at `split` or evenly into chunks of `size`. The position after an error is not compared.
    private static ReaderRun Run(string xml, int split, int size, bool doctype, XmlReaderLimits? limits = null)
    {
        limits ??= XmlReaderLimits.Default;
        var handler = doctype ? new DtdDoctypeHandler(limits) : null;
        var stack = handler is null ? null : new XmlEntityInputStack(limits, handler);
        var run = ReaderHarness.Run(xml, limits, split, size, handler, stack);

        return run.Status == XmlReadStatus.Error ? run with { Position = default } : run;
    }
}
