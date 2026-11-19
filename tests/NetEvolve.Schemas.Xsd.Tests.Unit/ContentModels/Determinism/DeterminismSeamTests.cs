namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Diagnostics;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Automaton.AutomatonTestKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism.DeterminismKit;

public class DeterminismSeamTests
{
    [Test]
    public async Task Seam_Checker_ReportsThroughTheReporter_AndReturnsTheVerdict()
    {
        var check = new DeterminismChecker();
        var ambiguous = Cho(El(A), El(A));
        var deterministic = Seq(El(A), El(B));
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);

        var first = check.Check(Compile(ambiguous), Terms(ambiguous), NameOf, reporter, Locate(99));
        var second = check.Check(Compile(deterministic), Terms(deterministic), NameOf, reporter, Locate(99));
        var issues = collector.ToResult().Issues;

        await Assert.That(check).IsAssignableTo<IDeterminismCheck>();
        await Assert.That(first).IsFalse();
        await Assert.That(second).IsTrue();
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosNonambig);
    }
}
