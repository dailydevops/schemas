namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary>
/// The function conversion rules as the string functions meet them (atomization of nodes, the cast of <c>xs:untypedAtomic</c>, the
/// promotion of <c>xs:anyURI</c>, XPTY0004), the string length limit, and the allocation budget of the search and counting functions.
/// </summary>
public class ConversionAndLimitTests
{
    [Test]
    public async Task AString_AnAnyUriArgument_IsPromotedToString() =>
        await Assert
            .That(Shown("upper-case", XdmSequence.Create(XdmItem.FromAnyUri("http://example.com/a"))))
            .IsEqualTo("string:HTTP://EXAMPLE.COM/A");

    [Test]
    public async Task AString_AnUntypedAtomicArgument_IsAcceptedAsTheString() =>
        await Assert
            .That(Shown("string-length", XdmSequence.Create(XdmItem.FromUntypedAtomic("abc"))))
            .IsEqualTo("integer:3");

    [Test]
    public async Task AString_ANodeArgument_IsAtomizedToItsStringValue()
    {
        var node = SequencesHarness.Parse("node:d1");

        await Assert.That(Shown("string-length", node)).IsEqualTo("integer:3");
        await Assert.That(Text("concat", node, Str("x"))).IsEqualTo("hitx");
        await Assert.That(Text("upper-case", node)).IsEqualTo("HIT");
    }

    [Test]
    [Arguments("upper-case")]
    [Arguments("lower-case")]
    [Arguments("string-length")]
    [Arguments("normalize-space")]
    [Arguments("string-to-codepoints")]
    public async Task AString_ABooleanOrIntegerArgument_IsXPTY0004(string local)
    {
        await Assert.That(Text(local, XdmSequence.Create(XdmItem.FromBoolean(true)))).IsEqualTo("error:XPTY0004");
        await Assert.That(Text(local, Int(1))).IsEqualTo("error:XPTY0004");
    }

    [Test]
    public async Task Concat_ABigStringAboveTheLimit_RaisesTheStringLengthError()
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(5) };
        var concat = SequencesHarness.Resolve("concat", 2);

        var raised = Raised(() => concat.Call(context, [Str("abc"), Str("def")]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
        await Assert.That(concat.Call(context, [Str("abc"), Str("de")])[0].TryGetString(out var fits)).IsTrue();
        await Assert.That(fits).IsEqualTo("abcde");
    }

    [Test]
    [Arguments("upper-case", "\U000000DF\U000000DF\U000000DF")]
    [Arguments("lower-case", "\U00000130\U00000130\U00000130")]
    public async Task CaseMapping_ThatGrowsTheStringAboveTheLimit_RaisesTheStringLengthError(string local, string text)
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(5) };
        var function = SequencesHarness.Resolve(local, 1);

        var raised = Raised(() => function.Call(context, [Str(text)]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
    }

    [Test]
    public async Task Join_ABigStringAboveTheLimit_RaisesTheStringLengthError()
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxStringLength(5) };
        var join = SequencesHarness.Resolve("string-join", 2);

        var raised = Raised(() => join.Call(context, [Strs("ab", "cd", "ef"), Str("-")]));

        await Assert.That(raised).IsEqualTo(XPathLimitErrors.StringLengthExceeded.ToString());
    }

    [Test]
    [Arguments("string-length", "abc\U0001F600")]
    [Arguments("string-length", "")]
    public async Task StringLength_AllocatesNothing(string local, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        XdmSequence[] arguments = [Str(text)];
        var function = SequencesHarness.Resolve(local, 1);
        using var context = new XPathEvaluationContext();

        AllocationAssert.NoAllocation(() => _ = function.Call(context, arguments), warmups: 50);

        await Assert.That(Shown(local, arguments)).IsEqualTo("integer:" + (text.Length == 0 ? 0 : 4));
    }

    [Test]
    [Arguments("contains", "att")]
    [Arguments("starts-with", "tat")]
    [Arguments("ends-with", "too")]
    public async Task Search_AllocatesNothing(string local, string value)
    {
        XdmSequence[] arguments = [Str("tattoo"), Str(value)];
        var function = SequencesHarness.Resolve(local, 2);
        using var context = new XPathEvaluationContext();

        AllocationAssert.NoAllocation(() => _ = function.Call(context, arguments), warmups: 50);

        await Assert.That(Shown(local, arguments)).IsEqualTo("boolean:true");
    }

    [Test]
    [Arguments("upper-case", "ABC")]
    [Arguments("lower-case", "abc")]
    [Arguments("normalize-space", "abc")]
    public async Task AFunctionThatChangesNothing_AllocatesNothing(string local, string text)
    {
        XdmSequence[] arguments = [Str(text)];
        var function = SequencesHarness.Resolve(local, 1);
        using var context = new XPathEvaluationContext();

        AllocationAssert.NoAllocation(() => _ = function.Call(context, arguments), warmups: 50);

        await Assert.That(Text(local, arguments)).IsEqualTo(text);
    }

    [Test]
    public async Task Concat_AllocatesAtMostTheResultString()
    {
        XdmSequence[] arguments = [Str("abc"), Str("def")];
        var function = SequencesHarness.Resolve("concat", 2);
        using var context = new XPathEvaluationContext();

        // The result string of six characters: header 22 bytes plus 14 bytes of characters, rounded up, and nothing for the scratch buffer.
        AllocationAssert.AtMost(48, () => _ = function.Call(context, arguments), warmups: 50);

        await Assert.That(Text("concat", arguments)).IsEqualTo("abcdef");
    }

    private static string Raised(System.Func<XdmSequence> call)
    {
        try
        {
            _ = call();
            return string.Empty;
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }
}
