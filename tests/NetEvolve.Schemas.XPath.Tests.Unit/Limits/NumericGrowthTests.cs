namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static LimitsFixture;

/// <summary>The numeric growth budgets (#639): the bit width of an integer beyond 64 bits and the scale of a decimal.</summary>
public class NumericGrowthTests
{
    private const string Max64 = "9223372036854775807";
    private const string Two64 = "18446744073709551616";

    [Test]
    public async Task Integer_ProductBeyondTheLimit_EndsInTheCodedDiagnostic()
    {
        var diagnostic = Evaluate($"{Max64} * {Max64}", XPathLimits.Default.WithMaxIntegerBits(100), out var result);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.IntegerWidthExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(diagnostic.Message).Contains("MaxIntegerBits");
        await Assert.That(diagnostic.Message).Contains("100");
        await Assert.That(result.IsEmpty).IsTrue();
    }

    [Test]
    [Arguments("4611686018427387904 * 4", 65, true)]
    [Arguments("4611686018427387904 * 4", 64, false)]
    [Arguments("-4611686018427387904 * 4", 64, false)]
    [Arguments("-4611686018427387904 * 4", 65, true)]
    [Arguments($"{Max64} + {Max64}", 64, true)]
    [Arguments($"-{Max64} - {Max64}", 64, true)]
    [Arguments($"{Two64} + {Two64}", 65, true)]
    [Arguments($"{Two64} + {Two64}", 64, false)]
    [Arguments($"{Two64} - -{Two64}", 64, false)]
    public async Task Integer_AtAndBeyondTheLimit(string expression, int bits, bool fits)
    {
        var diagnostic = Evaluate(expression, XPathLimits.Default.WithMaxIntegerBits(bits), out _);

        await Assert.That(diagnostic.Code).IsEqualTo(fits ? string.Empty : "XP0008");
    }

    [Test]
    public async Task Integer_RepeatedGrowth_StopsAtTheLimit()
    {
        var limits = XPathLimits.Default.WithMaxIntegerBits(200);

        var fits = Evaluate($"{Two64} * {Two64} * {Two64}", limits, out _);
        var breach = Evaluate($"{Two64} * {Two64} * {Two64} * {Two64}", limits, out _);

        await Assert.That(fits.Code).IsEqualTo(string.Empty);
        await Assert.That(breach.QName).IsEqualTo(XPathLimitErrors.IntegerWidthExceeded);
    }

    [Test]
    public async Task Integer_NarrowResults_AreNeverCharged()
    {
        var diagnostic = Evaluate(
            "3037000499 * 3037000499",
            XPathLimits.Default.WithMaxIntegerBits(64),
            out var result
        );

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Integer_DivisionAndRemainder_NeverGrow()
    {
        var limits = XPathLimits.Default.WithMaxIntegerBits(65);

        await Assert.That(Evaluate($"{Two64} idiv 3", limits, out _).Code).IsEqualTo(string.Empty);
        await Assert.That(Evaluate($"{Two64} mod 7", limits, out _).Code).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("0.0000001 * 0.0000001", 14, true)]
    [Arguments("0.0000001 * 0.0000001", 13, false)]
    [Arguments("1 + 0.00000000001", 11, true)]
    [Arguments("1 + 0.00000000001", 10, false)]
    [Arguments("0.00000000001 - 1", 10, false)]
    [Arguments("0.00000000001 mod 3", 10, false)]
    public async Task Decimal_AtAndBeyondTheLimit(string expression, int scale, bool fits)
    {
        var diagnostic = Evaluate(expression, XPathLimits.Default.WithMaxDecimalScale(scale), out _);

        await Assert.That(diagnostic.Code).IsEqualTo(fits ? string.Empty : "XP0009");
    }

    [Test]
    public async Task Decimal_BeyondTheLimit_NamesTheLimitAndTheValue()
    {
        var diagnostic = Evaluate("0.00000000001 + 1", XPathLimits.Default.WithMaxDecimalScale(3), out _);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.DecimalScaleExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(diagnostic.Message).Contains("MaxDecimalScale");
        await Assert.That(diagnostic.Message).Contains("3");
    }

    [Test]
    public async Task Decimal_DefaultLimit_AcceptsOrdinaryScales()
    {
        var diagnostic = Evaluate("1.5 * 2.25 + 0.001", XPathLimits.Default, out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Sum_BeyondTheLimit_EndsInTheCodedDiagnostic()
    {
        using var context = new XPathEvaluationContext { Limits = XPathLimits.Default.WithMaxIntegerBits(64) };
        var items = XdmSequence.Create([
            XdmItem.FromBigInteger(System.Numerics.BigInteger.Pow(2, 63)),
            XdmItem.FromBigInteger(System.Numerics.BigInteger.Pow(2, 63)),
        ]);

        XPathException? caught = null;
        try
        {
            _ = AggregateInput.Sum(items, context);
        }
        catch (XPathException exception)
        {
            caught = exception;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Diagnostic.QName).IsEqualTo(XPathLimitErrors.IntegerWidthExceeded);
    }
}
