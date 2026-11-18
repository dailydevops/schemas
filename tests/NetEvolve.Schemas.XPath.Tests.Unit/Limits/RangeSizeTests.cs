namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using static LimitsFixture;

/// <summary>
/// The range size budget. A range is charged by its size before anything is built, so <c>1 to 10000000000</c> ends in the diagnostic
/// even for a node that would produce its items lazily. The expression node is a test-local stand-in that calls the same helper as
/// the real range node of #226.
/// </summary>
public class RangeSizeTests
{
    [Test]
    public async Task Range_BeyondTheDefault_EndsInTheCodedDiagnostic_AndNotInOutOfMemory()
    {
        var diagnostic = Evaluate("1 to 10000000000", XPathLimits.Default, out var result);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RangeSizeExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Range_ExactlyAtTheLimit_Succeeds()
    {
        var diagnostic = Evaluate("1 to 10", XPathLimits.Default.WithMaxRangeSize(10), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(10);
    }

    [Test]
    public async Task Range_OneItemBeyondTheLimit_EndsInTheDiagnostic()
    {
        var diagnostic = Evaluate("1 to 11", XPathLimits.Default.WithMaxRangeSize(10), out _);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RangeSizeExceeded);
    }

    [Test]
    public async Task Range_BeyondTheLimit_NamesTheLimitAndTheValue_AndLocatesTheRange()
    {
        var diagnostic = Evaluate("(1 to 11)", XPathLimits.Default.WithMaxRangeSize(10), out _);

        await Assert.That(diagnostic.Message).Contains("MaxRangeSize");
        await Assert.That(diagnostic.Message).Contains("10");
        await Assert.That(diagnostic.Offset).IsEqualTo(1);
        await Assert.That(diagnostic.Length).IsEqualTo(7);
    }

    [Test]
    public async Task Range_StartAfterEnd_IsEmpty_WhateverTheLimit()
    {
        var diagnostic = Evaluate("10000000000 to 1", XPathLimits.Default.WithMaxRangeSize(1), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Range_SingleItem_FitsTheSmallestLimit()
    {
        var diagnostic = Evaluate("5 to 5", XPathLimits.Default.WithMaxRangeSize(1), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Range_OperandsBeyondSixtyFourBits_AreCharged_NotWrappedAround()
    {
        var wide = Evaluate("-99999999999999999999 to 99999999999999999999", XPathLimits.Default, out _);

        await Assert.That(wide.QName).IsEqualTo(XPathLimitErrors.RangeSizeExceeded);
    }

    // The helper the real range node calls; the span of a range is computed without overflow.
    [Test]
    [Arguments(long.MinValue, long.MaxValue)]
    [Arguments(0L, long.MaxValue)]
    [Arguments(long.MinValue, 0L)]
    [Arguments(1L, 11L)]
    public async Task ThrowIfRangeTooLarge_ForAnExtremeRange_RaisesInsteadOfOverflowing(long first, long last)
    {
        var limits = XPathLimits.Default.WithMaxRangeSize(10);

        await Assert
            .That(() => limits.ThrowIfRangeTooLarge(first, last, new SourceSpan(0, 1)))
            .Throws<XPathException>();
    }

    [Test]
    [Arguments(1L, 10L)]
    [Arguments(long.MaxValue, long.MaxValue)]
    [Arguments(long.MinValue, long.MinValue)]
    [Arguments(long.MaxValue - 9, long.MaxValue)]
    [Arguments(5L, 4L)]
    [Arguments(long.MaxValue, long.MinValue)]
    public async Task ThrowIfRangeTooLarge_WithinTheLimit_DoesNotRaise(long first, long last)
    {
        var limits = XPathLimits.Default.WithMaxRangeSize(10);

        limits.ThrowIfRangeTooLarge(first, last, new SourceSpan(0, 1));

        await Assert.That(limits.MaxRangeSize).IsEqualTo(10L);
    }

    [Test]
    public async Task ThrowIfRangeTooLarge_AtTheLargestLimit_AcceptsTheLargestRangeButOne()
    {
        var limits = XPathLimits.Default.WithMaxRangeSize(long.MaxValue);

        limits.ThrowIfRangeTooLarge(0L, long.MaxValue - 1, new SourceSpan(0, 1));
        var raised = ArithmeticFixture.Raised(() =>
        {
            limits.ThrowIfRangeTooLarge(long.MinValue, long.MaxValue, new SourceSpan(0, 1));
            return default;
        });

        await Assert.That(raised).IsNotNull();
    }
}
