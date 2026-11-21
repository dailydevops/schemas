namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

/// <summary>Baselines only shrink on purpose: a case that passes but is still listed fails the run.</summary>
public class Qt3BaselineShrinkTests
{
    private static Dictionary<string, KnownFailureBaseline> Baselines(params string[] ids) =>
        new(StringComparer.Ordinal) { ["set"] = KnownFailureBaseline.Parse(string.Join('\n', ids)) };

    private static List<CaseResult> Run(Qt3Assertion assertion, EngineResult result) =>
        Qt3Runner.Run([Case(assertion)], new FakeEngine(_ => result));

    [Test]
    public async Task Compare_CasePassesButIsListed_FailsTheRunAndNamesTheCase()
    {
        var results = Run(new Qt3Assertion("assert-true"), EngineResult.Value(true));

        var report = ConformanceReport.Compare(results, Baselines("set/c1"));

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.TestSets[0].UnexpectedPasses).IsEquivalentTo(["set/c1"]);
        await Assert.That(Qt3FailureMessage.Format(report.TestSets[0])).Contains("set/c1");
    }

    [Test]
    public async Task Compare_CaseFailsAndIsListed_Succeeds()
    {
        var results = Run(new Qt3Assertion("assert-true"), EngineResult.Value(false));

        await Assert.That(ConformanceReport.Compare(results, Baselines("set/c1")).IsSuccess).IsTrue();
    }

    [Test]
    public async Task Compare_CaseFailsAndIsNotListed_FailsTheRun()
    {
        var results = Run(new Qt3Assertion("assert-true"), EngineResult.Value(false));

        await Assert.That(ConformanceReport.Compare(results, Baselines()).IsSuccess).IsFalse();
    }
}
