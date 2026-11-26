namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class SchematronConformanceRunnerTests
{
    private static readonly BindingRule[] Rules =
    [
        new("xslt-1", "iso-schematron-xslt-1.xml"),
        new("xslt-2", "iso-schematron-xslt-2.xml"),
        new("xslt-3", "iso-schematron-xslt-3.xml", "xslt3 cases belong to SCH-21."),
    ];

    private static CaseSelection Selection(FixtureDirectory fixture) =>
        BindingFilter.Select(SchematronCatalogReader.Read(fixture.Path, Rules), Rules);

    [Test]
    public async Task Run_ObservedOutcomeEqualsExpected_IsPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(c => c.Expect);

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsEqualTo(5);
        await Assert
            .That(validator.Calls)
            .IsEquivalentTo([
                "set-a/one.xml@xslt-1",
                "set-a/one.xml@xslt-2",
                "set-a/two.xml@xslt-1",
                "set-a/two.xml@xslt-2",
                "set-b/only-xslt2.xml@xslt-2",
            ]);
    }

    [Test]
    public async Task Run_ObservedOutcomeDiffers_IsFailWithReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(_ => SchematronOutcome.Valid);

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var failed = results.Single(r => r.CaseId == "set-a/one.xml" && r.TestSet == "xslt-2.set-a");
        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results.Count(r => r.CaseId == "set-a/one.xml" && r.Outcome == CaseOutcome.Fail)).IsEqualTo(2);
        await Assert.That(failed.Reason).IsEqualTo("Expected Invalid but observed Valid.");
    }

    [Test]
    public async Task Run_OutcomeMatchesButObservationHasReason_IsFailWithThatReason(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new ReasonValidator(c => new SchematronObservation(c.Expect, "assertion 'x' does not hold."));

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var failed = results.First(r => string.Equals(r.CaseId, "set-a/one.xml", StringComparison.Ordinal));
        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(failed.Reason).IsEqualTo("assertion 'x' does not hold.");
    }

    [Test]
    public async Task Run_OutcomeDiffersAndObservationHasReason_IsFailWithBoth(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new ReasonValidator(_ => new SchematronObservation(SchematronOutcome.Valid, "no schema."));

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var failed = results.First(r => string.Equals(r.CaseId, "set-a/one.xml", StringComparison.Ordinal));
        await Assert.That(failed.Reason).IsEqualTo("Expected Invalid but observed Valid. no schema.");
    }

    [Test]
    public async Task Run_ExcludedCases_AreSkippedWithTheirReasonAndNeverValidated(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(c => c.Expect);

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var skipped = results.Single(r => r.Outcome == CaseOutcome.Skipped);
        await Assert.That(skipped.CaseId).IsEqualTo("set-b/only-xslt3.xml");
        await Assert.That(skipped.Reason).IsEqualTo("xslt3 cases belong to SCH-21.");
        await Assert.That(validator.Calls).DoesNotContain("set-b/only-xslt3.xml");
    }

    [Test]
    public async Task Run_UnboundValidator_FailsEverySelectedCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();

        var results = await SchematronConformanceRunner.RunAsync(
            Selection(fixture),
            UnboundSchematronValidator.Instance,
            cancellationToken
        );

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Fail)).IsEqualTo(5);
        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsEqualTo(0);
    }

    [Test]
    public async Task Compare_FailureNotInBaseline_IsRegression(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(_ => SchematronOutcome.Error);
        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert
            .That(report.TestSets.SelectMany(t => t.Regressions))
            .IsEquivalentTo(["set-a/one.xml", "set-a/one.xml", "set-a/two.xml", "set-a/two.xml"]);
    }

    [Test]
    public async Task Compare_PassOfBaselinedCase_IsUnexpectedPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(c => c.Expect);
        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                ["set-a"] = KnownFailureBaseline.Parse("set-a/one.xml\n"),
            }
        );

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.TestSets.SelectMany(t => t.UnexpectedPasses)).IsEquivalentTo(["set-a/one.xml"]);
    }

    [Test]
    public async Task Run_SameCaseUnderTwoBindings_HasOneTestSetPerBinding(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeSchematronValidator(c => c.Expect);

        var results = await SchematronConformanceRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        await Assert
            .That(results.Where(r => r.CaseId == "set-a/one.xml").Select(r => r.TestSet))
            .IsEquivalentTo(["xslt-1.set-a", "xslt-2.set-a"]);
    }

    private sealed class ReasonValidator(Func<SchematronCase, SchematronObservation> observe) : ISchematronValidator
    {
        public Task<SchematronObservation> ValidateAsync(
            SchematronCase schematronCase,
            string binding,
            CancellationToken cancellationToken
        ) => Task.FromResult(observe(schematronCase));
    }
}
