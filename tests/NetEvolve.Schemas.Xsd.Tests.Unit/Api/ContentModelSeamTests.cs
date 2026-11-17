namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class ContentModelSeamTests
{
    [Test]
    public async Task Seam_Matcher_StepsThroughTheSequence_AndEndsOnlyAtTheLastPosition()
    {
        var matcher = new FakeContentModelMatcher();
        ICompiledContentModel model = new FakeContentModel(10, 20);
        var state = matcher.Start;

        var endsEarly = matcher.CanEnd(model, state);
        var first = matcher.TryStep(model, ref state, 10, out var matchedFirst);
        var rejected = matcher.TryStep(model, ref state, 10, out var matchedRejected);
        var second = matcher.TryStep(model, ref state, 20, out var matchedSecond);
        var endsAtLast = matcher.CanEnd(model, state);

        await Assert.That(endsEarly).IsFalse();
        await Assert.That(first).IsTrue();
        await Assert.That(matchedFirst.ParticleIndex).IsEqualTo(0);
        await Assert.That(matchedFirst.IsWildcard).IsFalse();
        await Assert.That(rejected).IsFalse();
        await Assert.That(matchedRejected).IsEqualTo(default(MatchedParticle));
        await Assert.That(second).IsTrue();
        await Assert.That(matchedSecond.ParticleIndex).IsEqualTo(1);
        await Assert.That(endsAtLast).IsTrue();
        await Assert.That(state.Position).IsEqualTo(1);
    }

    [Test]
    public async Task Seam_Matcher_CollectExpected_IsBoundedByTheDestination()
    {
        var matcher = new FakeContentModelMatcher();
        ICompiledContentModel model = new FakeContentModel(10, 20);
        var state = matcher.Start;
        var (written, first, none) = Collect(matcher, model, state);

        await Assert.That(written).IsEqualTo(1);
        await Assert.That(first).IsEqualTo(10);
        await Assert.That(none).IsEqualTo(0);
    }

    [Test]
    public async Task Seam_Compiler_ReceivesTheGroupAndReturnsAProgramSizedByPositions()
    {
        var log = new List<string>();
        var group = new FakeComponent(XsdComponentKind.ModelGroup, "seq");
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var compiler = new FakeContentModelCompiler(new FakeContentModel(1, 2, 3), 10, log);

        var model = compiler.Compile(group, reporter);

        await Assert.That(string.Join("|", log)).IsEqualTo("Compile:seq");
        await Assert.That(model.PositionCount).IsEqualTo(3);
        await Assert.That(model.CounterSlots).IsEqualTo(0);
    }

    [Test]
    public async Task Seam_Compiler_ReportsTheEngineCodeWhenTheCeilingIsExceeded()
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var group = new FakeComponent(XsdComponentKind.ModelGroup, "big", line: 12);
        var compiler = new FakeContentModelCompiler(new FakeContentModel(1, 2, 3), 2, []);

        _ = compiler.Compile(group, new XsdIssueReporter(collector));
        var issues = collector.ToResult().Issues;

        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.ContentModelPositionCeilingExceeded);
        await Assert.That(issues[0].Location.Line).IsEqualTo(12);
    }

    private static (int Written, int First, int None) Collect(
        FakeContentModelMatcher matcher,
        ICompiledContentModel model,
        ContentModelState state
    )
    {
        Span<int> room = stackalloc int[4];
        var written = matcher.CollectExpected(model, state, room);
        var none = matcher.CollectExpected(model, state, Span<int>.Empty);

        return (written, room[0], none);
    }
}
