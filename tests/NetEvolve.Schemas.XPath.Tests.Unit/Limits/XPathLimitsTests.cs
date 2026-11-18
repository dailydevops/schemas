namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>The limits type: the pinned defaults, the flow from the core validation limits, the overrides, and the guards.</summary>
public class XPathLimitsTests
{
    // The defaults snapshot (D2 of the decisions on #229): depth from MaxDepth, every growth budget from MaxEntityExpansion.
    [Test]
    public async Task Default_PinsTheValueOfEveryBudget()
    {
        var limits = XPathLimits.Default;

        await Assert.That(limits.MaxRecursionDepth).IsEqualTo(256);
        await Assert.That(limits.MaxSequenceSize).IsEqualTo(10_000_000);
        await Assert.That(limits.MaxRangeSize).IsEqualTo(10_000_000L);
        await Assert.That(limits.MaxStringLength).IsEqualTo(10_000_000L);
    }

    [Test]
    public async Task Default_IsTheFlowOfTheDefaultValidationLimits()
    {
        var flowed = XPathLimits.From(ValidationLimits.Default);

        await Assert.That(XPathLimits.Default.MaxRecursionDepth).IsEqualTo(flowed.MaxRecursionDepth);
        await Assert.That(XPathLimits.Default.MaxSequenceSize).IsEqualTo(flowed.MaxSequenceSize);
        await Assert.That(XPathLimits.Default.MaxRangeSize).IsEqualTo(flowed.MaxRangeSize);
        await Assert.That(XPathLimits.Default.MaxStringLength).IsEqualTo(flowed.MaxStringLength);
    }

    [Test]
    public async Task Ceilings_ArePinned()
    {
        var depth = XPathLimits.MaxRecursionDepthCeiling;
        var sequence = XPathLimits.MaxSequenceSizeCeiling;

        await Assert.That(depth).IsEqualTo(ValidationLimits.MaxDepthCeiling);
        await Assert.That(sequence).IsEqualTo(2_147_483_591);
    }

    [Test]
    [Arguments(1, 1L)]
    [Arguments(7, 123L)]
    [Arguments(10_000, 2_000_000_000L)]
    public async Task From_ValuesOfTheCoreLimits_ReachTheBudgetsUnchanged(int maxDepth, long maxEntityExpansion)
    {
        var core = ValidationLimits.Default.WithMaxDepth(maxDepth).WithMaxEntityExpansion(maxEntityExpansion);

        var limits = XPathLimits.From(core);

        await Assert.That(limits.MaxRecursionDepth).IsEqualTo(maxDepth);
        await Assert.That(limits.MaxSequenceSize).IsEqualTo((int)maxEntityExpansion);
        await Assert.That(limits.MaxRangeSize).IsEqualTo(maxEntityExpansion);
        await Assert.That(limits.MaxStringLength).IsEqualTo(maxEntityExpansion);
    }

    [Test]
    public async Task From_ExpansionAboveTheArrayLimit_ClampsTheSequenceSizeOnly()
    {
        var core = ValidationLimits.Default.WithMaxEntityExpansion(5_000_000_000L);

        var limits = XPathLimits.From(core);

        await Assert.That(limits.MaxSequenceSize).IsEqualTo(XPathLimits.MaxSequenceSizeCeiling);
        await Assert.That(limits.MaxRangeSize).IsEqualTo(5_000_000_000L);
        await Assert.That(limits.MaxStringLength).IsEqualTo(5_000_000_000L);
    }

    [Test]
    public async Task EvaluationContext_ValuesSetOnTheCoreLimits_ReachTheEvaluatorUnchanged()
    {
        var core = ValidationLimits.Default.WithMaxDepth(9).WithMaxEntityExpansion(77);
        using var context = new XPathEvaluationContext { Limits = XPathLimits.From(core) };

        await Assert.That(context.Limits.MaxRecursionDepth).IsEqualTo(9);
        await Assert.That(context.Limits.MaxSequenceSize).IsEqualTo(77);
        await Assert.That(context.Limits.MaxRangeSize).IsEqualTo(77L);
        await Assert.That(context.Limits.MaxStringLength).IsEqualTo(77L);
    }

    [Test]
    public async Task EvaluationContext_StartsWithTheDefaults_AndKeepsTheLimitsThroughReset()
    {
        using var context = new XPathEvaluationContext();
        await Assert.That(context.Limits).IsSameReferenceAs(XPathLimits.Default);

        var custom = XPathLimits.Default.WithMaxRangeSize(5);
        context.Limits = custom;
        context.Reset();

        await Assert.That(context.Limits).IsSameReferenceAs(custom);
    }

    [Test]
    public async Task EvaluationContext_NullLimits_Throw()
    {
        using var context = new XPathEvaluationContext();

        await Assert.That(() => context.Limits = null!).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task From_NullLimits_Throws() =>
        await Assert.That(() => XPathLimits.From(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task With_OverridesOneBudget_AndLeavesTheOthersAndTheOriginal()
    {
        var original = XPathLimits.Default;

        var depth = original.WithMaxRecursionDepth(3);
        var sequence = original.WithMaxSequenceSize(4);
        var range = original.WithMaxRangeSize(5);
        var text = original.WithMaxStringLength(6);

        await Assert.That(depth.MaxRecursionDepth).IsEqualTo(3);
        await Assert.That(depth.MaxSequenceSize).IsEqualTo(original.MaxSequenceSize);
        await Assert.That(sequence.MaxSequenceSize).IsEqualTo(4);
        await Assert.That(sequence.MaxRangeSize).IsEqualTo(original.MaxRangeSize);
        await Assert.That(range.MaxRangeSize).IsEqualTo(5L);
        await Assert.That(range.MaxStringLength).IsEqualTo(original.MaxStringLength);
        await Assert.That(text.MaxStringLength).IsEqualTo(6L);
        await Assert.That(text.MaxRecursionDepth).IsEqualTo(original.MaxRecursionDepth);
        await Assert.That(original.MaxRecursionDepth).IsEqualTo(256);
        await Assert.That(original.MaxSequenceSize).IsEqualTo(10_000_000);
    }

    [Test]
    public async Task With_AtTheCeiling_IsAccepted()
    {
        var limits = XPathLimits
            .Default.WithMaxRecursionDepth(XPathLimits.MaxRecursionDepthCeiling)
            .WithMaxSequenceSize(XPathLimits.MaxSequenceSizeCeiling);

        await Assert.That(limits.MaxRecursionDepth).IsEqualTo(10_000);
        await Assert.That(limits.MaxSequenceSize).IsEqualTo(XPathLimits.MaxSequenceSizeCeiling);
    }

    [Test]
    public async Task With_ValueBelowOne_Throws()
    {
        var limits = XPathLimits.Default;

        await Assert.That(() => limits.WithMaxRecursionDepth(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => limits.WithMaxSequenceSize(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => limits.WithMaxRangeSize(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => limits.WithMaxStringLength(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task With_ValueAboveTheCeiling_Throws()
    {
        var limits = XPathLimits.Default;

        await Assert
            .That(() => limits.WithMaxRecursionDepth(XPathLimits.MaxRecursionDepthCeiling + 1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => limits.WithMaxSequenceSize(XPathLimits.MaxSequenceSizeCeiling + 1))
            .Throws<ArgumentOutOfRangeException>();
    }
}
