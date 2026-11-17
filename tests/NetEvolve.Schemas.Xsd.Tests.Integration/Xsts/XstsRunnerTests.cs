namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using NetEvolve.Schemas.Conformance.Results;

public class XstsRunnerTests
{
    private static XstsSelection Selection(FixtureDirectory fixture) =>
        XstsSelector.Select(
            XstsReader.Read(fixture.Path),
            new XstsFilter(XstsVersions.V11, XstsFilter.AcceptedStatuses)
        );

    [Test]
    public async Task Run_ObservedOutcomeEqualsExpected_IsPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var selection = Selection(fixture);
        var validator = new FakeXstsValidator(c => c.ExpectValid ? XstsOutcome.Valid : XstsOutcome.Invalid);

        var results = await XstsRunner.RunAsync(selection, validator, cancellationToken);

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Pass)).IsEqualTo(selection.Selected.Count);
        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Skipped)).IsEqualTo(selection.Excluded.Count);
        await Assert.That(results.Any(r => r.Outcome == CaseOutcome.Fail)).IsFalse();
    }

    [Test]
    public async Task Run_SchemaTestsCompile_InstanceTestsValidate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeXstsValidator(c => c.ExpectValid ? XstsOutcome.Valid : XstsOutcome.Invalid);

        _ = await XstsRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        await Assert.That(validator.Compiled).Contains("1.1/plain/schema");
        await Assert.That(validator.Compiled).DoesNotContain("1.1/plain/inst-valid");
        await Assert.That(validator.Validated).Contains("1.1/plain/inst-valid");
        await Assert.That(validator.Validated).DoesNotContain("1.1/plain/schema");
    }

    [Test]
    public async Task Run_ObservedOutcomeDiffers_IsFailWithReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeXstsValidator(_ => XstsOutcome.Valid);

        var results = await XstsRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var failed = results.Single(r => string.Equals(r.CaseId, "1.1/plain/inst-invalid", StringComparison.Ordinal));
        await Assert.That(failed.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(failed.TestSet).IsEqualTo("metaA-alpha");
        await Assert.That(failed.Reason).Contains("Invalid").And.Contains("Valid");
    }

    [Test]
    public async Task Run_ExcludedCases_AreSkippedWithTheirReasonAndNeverValidated(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var validator = new FakeXstsValidator(c => c.ExpectValid ? XstsOutcome.Valid : XstsOutcome.Invalid);

        var results = await XstsRunner.RunAsync(Selection(fixture), validator, cancellationToken);

        var skipped = results.Single(r => string.Equals(r.CaseId, "1.1/pd/schema", StringComparison.Ordinal));
        await Assert.That(skipped.Outcome).IsEqualTo(CaseOutcome.Skipped);
        await Assert.That(skipped.Reason).Contains("precisionDecimal");
        await Assert.That(validator.Compiled).DoesNotContain("1.1/pd/schema");
    }

    [Test]
    public async Task Run_UnboundValidator_FailsEverySelectedCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        var selection = Selection(fixture);

        var results = await XstsRunner.RunAsync(selection, UnboundXstsValidator.Instance, cancellationToken);

        await Assert.That(results.Count(r => r.Outcome == CaseOutcome.Fail)).IsEqualTo(selection.Selected.Count);
    }
}
