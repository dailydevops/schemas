namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:encode-for-uri</c> and <c>fn:iri-to-uri</c> (Functions and Operators 1.0, 8.4 and 8.5): the percent-encoding of the UTF-8
/// octets of a character, in upper-case hexadecimal, for every character of <c>encode-for-uri</c> but the unreserved ones and for
/// the characters of <c>iri-to-uri</c> that no URI may contain; an input without such a character is returned as the same string.
/// </summary>
public class UriEscapingTests
{
    private const string Unreserved = "abcXYZ019-_.~";
    private const string Reserved = "/?#[]@!$&'()*+,;=";

    private static string Run(string function, string text) => QNamesHarness.Run(function, "string:" + text);

    [Test]
    [Arguments(Unreserved, "string:" + Unreserved)]
    [Arguments("", "string:")]
    [Arguments(" ", "string:%20")]
    [Arguments("100% organic", "string:100%25%20organic")]
    [Arguments("http:examples", "string:http%3Aexamples")]
    [Arguments("http%20examples", "string:http%2520examples")]
    [Arguments(Reserved, "string:%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D")]
    [Arguments("<>\"{}|\\^`", "string:%3C%3E%22%7B%7D%7C%5C%5E%60")]
    [Arguments("\u0001\u007F", "string:%01%7F")]
    [Arguments("~b\u00E9b\u00E9", "string:~b%C3%A9b%C3%A9")]
    [Arguments("\u20AC", "string:%E2%82%AC")]
    [Arguments("\uD83D\uDE00", "string:%F0%9F%98%80")]
    [Arguments("a\uD800b", "string:a%EF%BF%BDb")]
    [Arguments("a\uDC00b", "string:a%EF%BF%BDb")]
    [Arguments("a\uD83D", "string:a%EF%BF%BD")]
    public async Task EncodeForUri_EncodesEveryCharacterButTheUnreservedOnes(string text, string expected) =>
        await Assert.That(Run("encode-for-uri", text)).IsEqualTo(expected);

    [Test]
    [Arguments(Unreserved, "string:" + Unreserved)]
    [Arguments("", "string:")]
    [Arguments(" ", "string:%20")]
    [Arguments("example%example", "string:example%example")]
    [Arguments(Reserved, "string:" + Reserved)]
    [Arguments("<>\"{}|\\^`", "string:%3C%3E%22%7B%7D%7C%5C%5E%60")]
    [Arguments("\u0001\u007F", "string:%01%7F")]
    [Arguments("http://www.example.com/~b\u00E9b\u00E9", "string:http://www.example.com/~b%C3%A9b%C3%A9")]
    [Arguments("\uD83D\uDE00", "string:%F0%9F%98%80")]
    [Arguments("a\uD800b", "string:a%EF%BF%BDb")]
    [Arguments("a\uD83D", "string:a%EF%BF%BD")]
    public async Task IriToUri_EscapesOnlyTheCharactersNoUriMayContain(string text, string expected) =>
        await Assert.That(Run("iri-to-uri", text)).IsEqualTo(expected);

    [Test]
    [Arguments("encode-for-uri")]
    [Arguments("iri-to-uri")]
    public async Task TheFunctions_TheEmptySequence_GivesTheEmptyString(string function) =>
        await Assert.That(QNamesHarness.Run(function, "()")).IsEqualTo("string:");

    [Test]
    [Arguments("encode-for-uri")]
    [Arguments("iri-to-uri")]
    public async Task TheFunctions_AUriOrUntypedArgument_IsReadAsAString(string function)
    {
        await Assert.That(QNamesHarness.Run(function, "anyURI:a b")).IsEqualTo("string:a%20b");
        await Assert.That(QNamesHarness.Run(function, "untypedAtomic:a b")).IsEqualTo("string:a%20b");
    }

    [Test]
    [Arguments("encode-for-uri")]
    [Arguments("iri-to-uri")]
    public async Task TheFunctions_AnArgumentOfTheWrongTypeOrCardinality_IsXPTY0004(string function)
    {
        await Assert.That(QNamesHarness.Run(function, "integer:12")).IsEqualTo("error:XPTY0004");
        await Assert.That(QNamesHarness.Run(function, "string:a|string:b")).IsEqualTo("error:XPTY0004");
    }

    [Test]
    [Arguments("encode-for-uri")]
    [Arguments("iri-to-uri")]
    public async Task TheFunctions_AnInputBeyondTheStackBuffer_IsEscapedInFull(string function)
    {
        // 400 characters of U+00E9 are 2 UTF-8 octets each: 6 characters of output per input character, beyond any stack buffer.
        var text = new string('\u00E9', 400);

        var result = Run(function, text);

        await Assert.That(result.Length).IsEqualTo("string:".Length + (400 * 6));
        await Assert.That(result.EndsWith("%C3%A9%C3%A9", StringComparison.Ordinal)).IsTrue();
        await Assert.That(result.StartsWith("string:%C3%A9", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("encode-for-uri", "abc-DEF_012.~")]
    [Arguments("iri-to-uri", "abc-DEF_012.~")]
    [Arguments("iri-to-uri", "http://www.example.com/a%20b?c=d&e=f#g")]
    public async Task TheFunctions_AnInputWithoutACharacterToEscape_IsReturnedAsTheSameString(
        string function,
        string text
    )
    {
        var input = new string(text.AsSpan());

        var output = Call(function, input);

        await Assert.That(ReferenceEquals(output, input)).IsTrue();
    }

    [Test]
    [Arguments("encode-for-uri")]
    [Arguments("iri-to-uri")]
    public async Task TheFunctions_AnInputWithACharacterToEscape_IsANewString(string function)
    {
        var input = new string("a b".AsSpan());

        var output = Call(function, input);

        await Assert.That(ReferenceEquals(output, input)).IsFalse();
        await Assert.That(output).IsEqualTo("a%20b");
    }

    private static string? Call(string function, string input)
    {
        _ = QNamesHarness.TryResolve(function, 1, out var resolved);
        using var context = new XPathEvaluationContext();

        var result = resolved!.Call(context, [XdmSequence.Create(XdmItem.FromString(input))]);

        _ = result[0].TryGetString(out var output);
        return output;
    }
}
