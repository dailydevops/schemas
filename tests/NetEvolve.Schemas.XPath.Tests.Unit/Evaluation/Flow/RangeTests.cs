namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static FlowFixture;

/// <summary>The range expression (XPath 2.0, 3.3.1): the integers from the first operand to the second, produced lazily, charged by their number.</summary>
public class RangeTests
{
    [Test]
    [Arguments("1 to 3", "integer:1, integer:2, integer:3")]
    [Arguments("3 to 1", "()")]
    [Arguments("2 to 2", "integer:2")]
    [Arguments("-2 to 1", "integer:-2, integer:-1, integer:0, integer:1")]
    [Arguments("() to 3", "()")]
    [Arguments("1 to ()", "()")]
    [Arguments("() to ()", "()")]
    [Arguments("$untyped to 4", "integer:2, integer:3, integer:4")]
    [Arguments("$host to 98", "()")]
    [Arguments("(1 to 2, 5 to 4, 7 to 7)", "integer:1, integer:2, integer:7")]
    public async Task ProducesTheIntegersInOrder(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments(
        "9223372036854775806 to 9223372036854775807",
        "integer:9223372036854775806, integer:9223372036854775807"
    )]
    [Arguments(
        "-9223372036854775808 to -9223372036854775807",
        "integer:-9223372036854775808, integer:-9223372036854775807"
    )]
    [Arguments("9223372036854775807 to 9223372036854775807", "integer:9223372036854775807")]
    public async Task ARangeAtTheEdgeOfTheSixtyFourBitIntegers_DoesNotOverflow(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("99999999999999999999 to 99999999999999999998", "()")]
    [Arguments("99999999999999999999 to 99999999999999999999", "integer:99999999999999999999")]
    [Arguments(
        "-99999999999999999999 to -99999999999999999998",
        "integer:-99999999999999999999, integer:-99999999999999999998"
    )]
    [Arguments("99999999999999999999 to 1", "()")]
    [Arguments("1 to -99999999999999999999", "()")]
    [Arguments(
        "9223372036854775806 to 9223372036854775809",
        "integer:9223372036854775806, integer:9223372036854775807, integer:9223372036854775808, integer:9223372036854775809"
    )]
    public async Task AnOperandBeyondSixtyFourBits_GivesTheExactRange(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("1 to 99999999999999999999")]
    [Arguments("-99999999999999999999 to 1")]
    [Arguments("-99999999999999999999 to 99999999999999999999")]
    public async Task ARangeBeyondTheLimit_WithAnOperandBeyondSixtyFourBits_RaisesTheRangeSizeError(
        string expression
    ) => await Assert.That(Code(expression)).IsEqualTo(XPathLimitErrors.RangeSizeExceeded.LocalName);

    [Test]
    [Arguments("(1, 2) to 3", "XPTY0004")]
    [Arguments("1 to (1, 2)", "XPTY0004")]
    [Arguments("1.5 to 3", "XPTY0004")]
    [Arguments("1 to 3.0", "XPTY0004")]
    [Arguments("1e0 to 3", "XPTY0004")]
    [Arguments("'1' to 3", "XPTY0004")]
    [Arguments("$word to 3", "XPTY0004")]
    [Arguments("$node to 3", "FORG0001")]
    [Arguments("$badUntyped to 3", "FORG0001")]
    public async Task AnOperandThatIsNotAnInteger_RaisesAnError(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    public async Task ARangeWithTheLimitItems_Succeeds()
    {
        var limits = XPathLimits.Default.WithMaxRangeSize(10);

        await Assert.That(Evaluate("1 to 10", limits: limits).Count).IsEqualTo(10);
        await Assert.That(Code("1 to 11", limits: limits)).IsEqualTo(XPathLimitErrors.RangeSizeExceeded.LocalName);
        await Assert.That(Code("11 to 1", limits: limits)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TheRangeSizeError_LocatesTheRangeExpression()
    {
        var compiled = Compile("(1 to 11)");
        using var context = NewContext(XPathLimits.Default.WithMaxRangeSize(10));

        var error = Assert.Throws<XPathException>(() => compiled.Evaluate(context));

        await Assert.That(error.Diagnostic.Offset).IsEqualTo(1);
        await Assert.That(error.Diagnostic.Length).IsEqualTo(7);
    }

    [Test]
    public async Task AHugeRange_IsNotMaterialized_WhenOnlyTheFirstItemIsTaken()
    {
        var compiled = Compile("1 to 1000000000");
        using var context = NewContext(XPathLimits.Default.WithMaxRangeSize(1_000_000_000));
        var hasItems = false;

        // Building the 1,000,000,000 items would take 24 GB; the sequence holds the two bounds and nothing else.
        AllocationAssert.AtMost(256, () => hasItems = compiled.Evaluate(context).HasItems(), warmups: 50);

        await Assert.That(hasItems).IsTrue();
    }

    [Test]
    public async Task AHugeRange_IsReadOnlyWhenTheConsumerReadsIt()
    {
        var compiled = Compile("1 to 1000");
        using var context = NewContext();

        var result = compiled.Evaluate(context);

        await Assert.That(result.CountItems()).IsEqualTo(1000);
        await Assert.That(result[999]).IsEqualTo(XdmItem.FromInteger(1000));
        await Assert.That(result.Count).IsEqualTo(1000);
    }
}
