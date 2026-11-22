namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

using System;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Documents;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The text functions reach text resources only through the resolver of the evaluation context. The compiler enables XPath 2.0 only, so a
/// call in XPath 3.0 or 3.1 goes through the registry of the default catalog, and XPath 2.0 through a compiled expression.
/// </summary>
public class UnparsedTextTests
{
    private const string T = "http://example.com/dir/t.txt";
    private const char Nel = (char)0x85;
    private const char LineSeparator = (char)0x2028;

    private static string Run(string local, IXPathResourceResolver? resolver, params string[] arguments)
    {
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        return RunIn(local, context, arguments);
    }

    private static string RunIn(string local, XPathEvaluationContext context, params string[] arguments) =>
        DocumentsHarness.RunFunction(local, XPathVersion.V30, XPathHostRequirements.None, context, arguments);

    private static DocumentsHarness.FakeResolver Serving(string text) =>
        new DocumentsHarness.FakeResolver().Add(T, text);

    private static byte[] Bytes(byte[] prefix, Encoding encoding, string text)
    {
        var body = encoding.GetBytes(text);
        var all = new byte[prefix.Length + body.Length];
        prefix.CopyTo(all, 0);
        body.CopyTo(all, prefix.Length);
        return all;
    }

    [Test]
    public async Task DefaultResolver_UnparsedTextAndLinesRaiseFOUT1170_AndAvailableIsFalse()
    {
        var deny = XPathResourceResolverAdapter.Wrap(DenyAllResourceResolver.Instance);

        foreach (var resolver in new IXPathResourceResolver?[] { null, deny })
        {
            await Assert.That(Run("unparsed-text", resolver, T)).IsEqualTo("error:FOUT1170");
            await Assert.That(Run("unparsed-text-lines", resolver, T)).IsEqualTo("error:FOUT1170");
            await Assert.That(Run("unparsed-text-available", resolver, T)).IsEqualTo("false");
        }
    }

    [Test]
    public async Task UnparsedText_IsUnknownInPlainXPath20_AndKnownWithTheXslt20HostFlag()
    {
        var resolver = Serving("hello");

        await Assert.That(DocumentsHarness.Run("unparsed-text('" + T + "')", resolver)).IsEqualTo("error:XPST0017");
        await Assert
            .That(DocumentsHarness.Run("unparsed-text-available('" + T + "')", resolver))
            .IsEqualTo("error:XPST0017");
        await Assert
            .That(DocumentsHarness.Run("unparsed-text('" + T + "')", resolver, host: XPathHostRequirements.Xslt20Host))
            .IsEqualTo("hello");
        await Assert
            .That(
                DocumentsHarness.Run(
                    "unparsed-text-available('" + T + "')",
                    resolver,
                    host: XPathHostRequirements.Xslt20Host
                )
            )
            .IsEqualTo("true");
    }

    [Test]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task UnparsedText_IsAvailableInXPath30And31WithoutAHostFlag(XPathVersion version)
    {
        using var context = new XPathEvaluationContext { ResourceResolver = Serving("hello") };
        const XPathHostRequirements none = XPathHostRequirements.None;

        await Assert.That(DocumentsHarness.RunFunction("unparsed-text", version, none, context, T)).IsEqualTo("hello");
        await Assert
            .That(DocumentsHarness.RunFunction("unparsed-text-available", version, none, context, T))
            .IsEqualTo("true");
    }

    [Test]
    [Arguments(XPathVersion.V30)]
    [Arguments(XPathVersion.V31)]
    public async Task UnparsedTextLines_IsAvailableFrom30Only_AlsoWithTheXslt20HostFlag(XPathVersion version)
    {
        var resolver = Serving("a\nb");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert
            .That(DocumentsHarness.Run("unparsed-text-lines('" + T + "')", resolver))
            .IsEqualTo("error:XPST0017");
        await Assert
            .That(
                DocumentsHarness.Run(
                    "unparsed-text-lines('" + T + "')",
                    resolver,
                    host: XPathHostRequirements.Xslt20Host
                )
            )
            .IsEqualTo("error:XPST0017");
        await Assert
            .That(DocumentsHarness.RunFunction("unparsed-text-lines", version, XPathHostRequirements.None, context, T))
            .IsEqualTo("a|b");
    }

    [Test]
    [Arguments("unparsed-text", XPathVersion.V30)]
    [Arguments("unparsed-text-available", XPathVersion.V30)]
    [Arguments("unparsed-text-lines", XPathVersion.V31)]
    public async Task Registration_TakesOneOrTwoArguments(string local, XPathVersion version)
    {
        await Assert.That(DocumentsHarness.TryResolve(local, 0, out _, version)).IsFalse();
        await Assert.That(DocumentsHarness.TryResolve(local, 1, out _, version)).IsTrue();
        await Assert.That(DocumentsHarness.TryResolve(local, 2, out _, version)).IsTrue();
        await Assert.That(DocumentsHarness.TryResolve(local, 3, out _, version)).IsFalse();
    }

    [Test]
    public async Task UnparsedText_ReturnsTheDecodedText_ThroughAnXPathTextRequest()
    {
        var resolver = Serving("one\r\ntwo");

        await Assert.That(Run("unparsed-text", resolver, T)).IsEqualTo("one\r\ntwo");
        await Assert.That(resolver.Requests[0]).IsEqualTo((ResourceKind.XPathText, T, (string?)null, (string?)T));
    }

    [Test]
    public async Task UnparsedText_IsPooled_SoAvailableThenTextThenLinesAskTheResolverOnce()
    {
        var resolver = Serving("a\nb");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(RunIn("unparsed-text-available", context, T)).IsEqualTo("true");
        await Assert.That(RunIn("unparsed-text", context, T)).IsEqualTo("a\nb");
        await Assert.That(RunIn("unparsed-text-lines", context, T)).IsEqualTo("a|b");
        await Assert.That(resolver.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task UnparsedText_ResolverMissAndDenial_RaiseFOUT1170AndAvailableIsFalse()
    {
        var resolver = new DocumentsHarness.FakeResolver().Deny(T);

        await Assert.That(Run("unparsed-text", resolver, T)).IsEqualTo("error:FOUT1170");
        await Assert.That(Run("unparsed-text-available", resolver, T)).IsEqualTo("false");
        await Assert.That(Run("unparsed-text", resolver, "http://example.com/none.txt")).IsEqualTo("error:FOUT1170");
        await Assert
            .That(Run("unparsed-text-lines", resolver, "http://example.com/none.txt"))
            .IsEqualTo("error:FOUT1170");
    }

    [Test]
    [Arguments("a b")]
    [Arguments("%gg")]
    [Arguments("http://example.com/dir/t.txt#frag")]
    [Arguments("t.txt")]
    public async Task UnparsedText_ReferenceThatIsNoUriOrIsRelativeWithoutBase_RaisesFOUT1170WithoutAResolverCall(
        string reference
    )
    {
        var resolver = Serving("x");

        await Assert.That(Run("unparsed-text", resolver, reference)).IsEqualTo("error:FOUT1170");
        await Assert.That(Run("unparsed-text-lines", resolver, reference)).IsEqualTo("error:FOUT1170");
        await Assert.That(Run("unparsed-text-available", resolver, reference)).IsEqualTo("false");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task UnparsedText_EmptyArgument_GivesTheEmptySequenceAndAvailableIsFalse()
    {
        var resolver = Serving("x");

        await Assert.That(Run("unparsed-text", resolver, "()")).IsEmpty();
        await Assert.That(Run("unparsed-text-lines", resolver, "()")).IsEmpty();
        await Assert.That(Run("unparsed-text-available", resolver, "()")).IsEqualTo("false");
        await Assert.That(resolver.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task UnparsedText_RelativeReference_ResolvesAgainstTheStaticBaseUriOfTheFunction()
    {
        var resolver = Serving("based");
        var function = new UnparsedTextFunction(new Uri("http://example.com/dir/"));
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };

        await Assert.That(DocumentsHarness.Call(function, context, "t.txt")).IsEqualTo("based");
        await Assert
            .That(resolver.Requests[0])
            .IsEqualTo((ResourceKind.XPathText, "t.txt", (string?)"http://example.com/dir/", (string?)T));
    }

    [Test]
    public async Task UnparsedText_DecodesTheByteOrderMarkEncodingsAndStripsTheMark()
    {
        const string text = "héllo";
        var resolver = new DocumentsHarness.FakeResolver()
            .Add("http://example.com/utf8", Bytes([0xEF, 0xBB, 0xBF], new UTF8Encoding(false), text))
            .Add("http://example.com/le", Bytes([0xFF, 0xFE], new UnicodeEncoding(false, false), text))
            .Add("http://example.com/be", Bytes([0xFE, 0xFF], new UnicodeEncoding(true, false), text));

        await Assert.That(Run("unparsed-text", resolver, "http://example.com/utf8")).IsEqualTo(text);
        await Assert.That(Run("unparsed-text", resolver, "http://example.com/le")).IsEqualTo(text);
        await Assert.That(Run("unparsed-text", resolver, "http://example.com/be")).IsEqualTo(text);
    }

    [Test]
    public async Task UnparsedText_ExplicitEncoding_DecodesWithThatEncoding()
    {
        const string text = "héy";
        var resolver = new DocumentsHarness.FakeResolver()
            .Add("http://example.com/latin1", new byte[] { (byte)'h', 0xE9, (byte)'y' })
            .Add("http://example.com/le", Bytes([], new UnicodeEncoding(false, false), text))
            .Add("http://example.com/utf8", Bytes([0xEF, 0xBB, 0xBF], new UTF8Encoding(false), text));

        await Assert.That(Run("unparsed-text", resolver, "http://example.com/latin1", "ISO-8859-1")).IsEqualTo(text);
        await Assert.That(Run("unparsed-text", resolver, "http://example.com/le", "utf-16le")).IsEqualTo(text);
        await Assert.That(Run("unparsed-text", resolver, "http://example.com/utf8", "UTF-8")).IsEqualTo(text);
        await Assert
            .That(Run("unparsed-text-available", resolver, "http://example.com/latin1", "iso-8859-1"))
            .IsEqualTo("true");
    }

    [Test]
    public async Task UnparsedText_WrongEncoding_RaisesFOUT1190AndAvailableIsFalse()
    {
        const string latin1 = "http://example.com/latin1";
        var resolver = new DocumentsHarness.FakeResolver()
            .Add(latin1, new byte[] { (byte)'h', 0xE9, (byte)'y' })
            .Add("http://example.com/ascii", new byte[] { 0x80 });

        // An unknown name, bytes that are no UTF-8 (without and with the encoding named), and a byte that is no ASCII.
        await Assert.That(Run("unparsed-text", resolver, latin1, "123")).IsEqualTo("error:FOUT1190");
        await Assert.That(Run("unparsed-text", resolver, latin1)).IsEqualTo("error:FOUT1190");
        await Assert.That(Run("unparsed-text", resolver, latin1, "UTF-8")).IsEqualTo("error:FOUT1190");
        await Assert
            .That(Run("unparsed-text", resolver, "http://example.com/ascii", "us-ascii"))
            .IsEqualTo("error:FOUT1190");
        await Assert.That(Run("unparsed-text-lines", resolver, latin1)).IsEqualTo("error:FOUT1190");
        await Assert.That(Run("unparsed-text-available", resolver, latin1, "123")).IsEqualTo("false");
        await Assert.That(Run("unparsed-text-available", resolver, latin1)).IsEqualTo("false");
    }

    [Test]
    public async Task UnparsedText_ACharacterThatIsNoXmlCharacter_RaisesFOUT1200AndAvailableIsFalse()
    {
        const string control = "http://example.com/ctrl";
        var resolver = new DocumentsHarness.FakeResolver().Add(control, new byte[] { (byte)'a', 0x01, (byte)'b' });

        await Assert.That(Run("unparsed-text", resolver, control)).IsEqualTo("error:FOUT1200");
        await Assert.That(Run("unparsed-text-lines", resolver, control)).IsEqualTo("error:FOUT1200");
        await Assert.That(Run("unparsed-text-available", resolver, control)).IsEqualTo("false");
    }

    [Test]
    public async Task UnparsedText_CharactersBeyondTheBasicPlane_ArePreserved()
    {
        var text = "a" + char.ConvertFromUtf32(0x1F600) + "b";
        var resolver = Serving(text);

        await Assert.That(Run("unparsed-text", resolver, T)).IsEqualTo(text);
    }

    [Test]
    [Arguments("a\nb", "a|b")]
    [Arguments("a\rb", "a|b")]
    [Arguments("a\r\nb", "a|b")]
    [Arguments("a\n\rb", "a||b")]
    [Arguments("a\r\n\r\nb\n\nc", "a||b||c")]
    [Arguments("one\n", "one")]
    [Arguments("one\r\n", "one")]
    [Arguments("one\n\n", "one|")]
    [Arguments("\nx", "|x")]
    [Arguments("", "")]
    [Arguments("\n", "")]
    public async Task UnparsedTextLines_SplitsOnLineFeedCarriageReturnAndBoth(string text, string expected)
    {
        var resolver = Serving(text);

        await Assert.That(Run("unparsed-text-lines", resolver, T)).IsEqualTo(expected);
    }

    [Test]
    public async Task UnparsedTextLines_NextLineAndLineSeparatorAreNoLineEnding()
    {
        var text = "a" + Nel + "b" + LineSeparator + "c";
        var resolver = Serving(text);

        await Assert.That(Run("unparsed-text-lines", resolver, T)).IsEqualTo(text);
    }

    [Test]
    public async Task UnparsedTextLines_YieldsOneItemPerLine_AndNoneForTheEmptyText()
    {
        var function = new UnparsedTextLinesFunction();
        using var context = new XPathEvaluationContext { ResourceResolver = Serving("x\r\n\r\ny\rz\n") };
        using var empty = new XPathEvaluationContext { ResourceResolver = Serving(string.Empty) };

        var lines = function.Call(context, [XdmSequence.Create(XdmItem.FromString(T))]);
        var none = function.Call(empty, [XdmSequence.Create(XdmItem.FromString(T))]);

        await Assert.That(lines.Count).IsEqualTo(4);
        await Assert.That(none.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UnparsedText_ResourceBeyondTheResourceLimit_RaisesFOUT1170()
    {
        var resolver = new DocumentsHarness.FakeResolver();
        for (var i = 0; i < 17; i++)
        {
            _ = resolver.Add("http://example.com/t" + i, "x");
        }

        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        for (var i = 0; i < 16; i++)
        {
            await Assert.That(RunIn("unparsed-text", context, "http://example.com/t" + i)).IsEqualTo("x");
        }

        await Assert.That(RunIn("unparsed-text", context, "http://example.com/t16")).IsEqualTo("error:FOUT1170");
        await Assert.That(resolver.Calls).IsEqualTo(16);
    }

    [Test]
    public async Task UnparsedText_CountsAgainstTheSameResourceLimitAsDocuments()
    {
        var resolver = new DocumentsHarness.FakeResolver();
        for (var i = 0; i < 16; i++)
        {
            _ = resolver.Add("http://example.com/d" + i, "<r/>");
        }

        _ = resolver.Add("http://example.com/t", "x");
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        for (var i = 0; i < 16; i++)
        {
            _ = DocumentsHarness.Run("doc('http://example.com/d" + i + "')", context);
        }

        await Assert.That(RunIn("unparsed-text", context, "http://example.com/t")).IsEqualTo("error:FOUT1170");
        await Assert.That(DocumentsHarness.Run("doc('http://example.com/d0')", context)).IsEqualTo("doc");
    }

    [Test]
    public async Task UnparsedText_ResultIsOneStringItem()
    {
        using var context = new XPathEvaluationContext { ResourceResolver = Serving("x") };
        var function = new UnparsedTextFunction(null);

        var result = function.Call(context, [XdmSequence.Create(XdmItem.FromString(T))]);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetString(out var text)).IsTrue();
        await Assert.That(text).IsEqualTo("x");
    }
}
