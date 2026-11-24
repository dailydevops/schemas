namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Delta3;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The string functions of XPath 3.0 and 3.1 (Functions and Operators 3.1, 5.4.2, 5.6.2, and 5.6.4): the one-argument
/// <c>fn:string-join</c> and <c>fn:tokenize</c>, and <c>fn:contains-token</c>.
/// </summary>
public class StringFunctionTests
{
    private const string Codepoint = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    [Test]
    [Arguments("string-join", 1)]
    [Arguments("tokenize", 1)]
    [Arguments("contains-token", 2)]
    [Arguments("contains-token", 3)]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(
        string local,
        int arity,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V20)).IsFalse();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V30)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V31)).IsTrue();
    }

    [Test]
    public async Task Registry_TheTwoArgumentStringJoinAndTheTwoAndThreeArgumentTokenize_StayInXPath20(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.TryResolve("string-join", 2, out _, XPathVersion.V20)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve("string-join", 2, out _, XPathVersion.V31)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve("tokenize", 2, out _, XPathVersion.V20)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve("tokenize", 3, out _, XPathVersion.V20)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve("contains-token", 4, out _, XPathVersion.V31)).IsFalse();
    }

    [Test]
    [Arguments("fn:string-join(('a'))")]
    [Arguments("fn:tokenize('a')")]
    [Arguments("fn:contains-token('a', 'a')")]
    public async Task TryCompile_InXPath20_ReturnsXpst0017(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V20)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .Build();

        await Assert.That(XPathExpression.TryCompile(query, statics).Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    [Arguments("a|b|c", "abc")]
    [Arguments("a", "a")]
    [Arguments("", "")]
    [Arguments("|", "")]
    [Arguments("ab|", "ab")]
    public async Task StringJoin_OneArgument_ConcatenatesTheStrings(
        string values,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(values);

        var input = values.Length == 0 ? XdmSequence.Empty : Delta3Harness.Str(values.Split('|'));

        await Assert.That(Delta3Harness.Show(Delta3Harness.Call("string-join", input))).IsEqualTo(expected);
    }

    [Test]
    public async Task StringJoin_OneArgument_OfAnotherType_RaisesXPTY0004(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.ErrorOf("string-join", Delta3Harness.Ints(1))).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("  red  green\tblue\n", "red|green|blue")]
    [Arguments("single", "single")]
    [Arguments("a\r\nb", "a|b")]
    [Arguments("", "")]
    [Arguments(" \t\r\n ", "")]
    public async Task Tokenize_OneArgument_SplitsAtWhitespace(
        string input,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("tokenize", Delta3Harness.Str(input))))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task Tokenize_OneArgument_ForTheEmptySequence_ReturnsTheEmptySequence(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.Call("tokenize", XdmSequence.Empty).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("red green blue ", "red", "true")]
    [Arguments("red green blue ", " red ", "true")]
    [Arguments("red green blue", "blue", "true")]
    [Arguments("red, green, blue", "red", "false")]
    [Arguments("red green", "re", "false")]
    [Arguments("red green", "red green", "false")]
    [Arguments("red green", "", "false")]
    [Arguments("red green", "  ", "false")]
    [Arguments("", "red", "false")]
    [Arguments("Red", "red", "false")]
    public async Task ContainsToken_TestsForOneWhitespaceSeparatedToken(
        string input,
        string token,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Delta3Harness.Call("contains-token", Delta3Harness.Str(input), Delta3Harness.Str(token));

        await Assert.That(Delta3Harness.Show(result)).IsEqualTo(expected);
    }

    [Test]
    public async Task ContainsToken_ForASequence_TestsEveryString(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var token = Delta3Harness.Str(" red ");

        await Assert
            .That(
                Delta3Harness.Show(
                    Delta3Harness.Call("contains-token", Delta3Harness.Str("red", "green", "blue"), token)
                )
            )
            .IsEqualTo("true");
        await Assert
            .That(
                Delta3Harness.Show(
                    Delta3Harness.Call(
                        "contains-token",
                        Delta3Harness.Str("x y", "green red"),
                        Delta3Harness.Str("red")
                    )
                )
            )
            .IsEqualTo("true");
        await Assert
            .That(
                Delta3Harness.Show(
                    Delta3Harness.Call("contains-token", Delta3Harness.Str("green", "blue"), Delta3Harness.Str("red"))
                )
            )
            .IsEqualTo("false");
        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("contains-token", XdmSequence.Empty, Delta3Harness.Str("red"))))
            .IsEqualTo("false");
    }

    [Test]
    public async Task ContainsToken_WithACollation_AcceptsTheCodepointCollationOnly(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var input = Delta3Harness.Str("red green");
        var token = Delta3Harness.Str("green");

        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("contains-token", input, token, Delta3Harness.Str(Codepoint))))
            .IsEqualTo("true");
        await Assert
            .That(Delta3Harness.ErrorOf("contains-token", input, token, Delta3Harness.Str("urn:other")))
            .IsEqualTo("FOCH0002");
    }

    [Test]
    public async Task ContainsToken_ForAnEmptyToken_RaisesXPTY0004(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(Delta3Harness.ErrorOf("contains-token", Delta3Harness.Str("a"), XdmSequence.Empty))
            .IsEqualTo("XPTY0004");
    }
}
