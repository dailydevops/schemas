namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

public class DifferentialRunnerTests
{
    private static readonly DifferentialEnvironment BaselineEnvironment = new("net10.0", "10.0");
    private static readonly DifferentialEnvironment OtherEnvironment = new("net6.0", "9.0");
    private static readonly DeviationList NoDeviations = new("10.0", []);

    private static DifferentialCase Case(string id, Verdict schema = Verdict.Valid, Verdict instance = Verdict.Valid) =>
        new(id, ["a.xsd"], instance == Verdict.NotRun ? null : "a.xml", schema, instance);

    private static EngineResult Result(Verdict schema, Verdict instance, params IssuePosition[] issues) =>
        new(schema, instance, issues);

    private static FakeDifferentialEngine Engine(EngineResult result) => new(_ => result);

    private static DeviationEntry Entry(string caseId, DifferenceKind kind, string observed) =>
        new(caseId, kind, observed, "XSD 1.0 Part 1, 3.4.4", "The baseline rejects a legal particle.");

    private static Task<DifferentialReport> RunAsync(
        DifferentialCase[] cases,
        FakeDifferentialEngine ours,
        FakeDifferentialEngine baseline,
        DeviationList deviations,
        DifferentialEnvironment environment,
        CancellationToken cancellationToken
    ) => DifferentialRunner.RunAsync(cases, ours, baseline, deviations, environment, cancellationToken);

    [Test]
    public async Task Run_EqualVerdictsAndPositions_Passes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 5));
        var baseline = Engine(result);

        var report = await RunAsync(
            [Case("c1", instance: Verdict.Invalid)],
            Engine(result),
            baseline,
            NoDeviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.BaselineRun).IsTrue();
        await Assert.That(baseline.Calls).IsEquivalentTo(["c1"]);
    }

    [Test]
    public async Task Run_InstanceVerdictDifferenceWithoutEntry_FailsAndNamesTheCase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await RunAsync(
            [Case("set/group/c1")],
            Engine(Result(Verdict.Valid, Verdict.Valid)),
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(2, 1))),
            NoDeviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.IsSuccess).IsFalse();
        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("set/group/c1").And.Contains("InstanceVerdict");
    }

    [Test]
    public async Task Run_SchemaVerdictDifferenceWithoutEntry_FailsAndNamesTheCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await RunAsync(
            [Case("c1")],
            Engine(Result(Verdict.Invalid, Verdict.NotRun)),
            Engine(Result(Verdict.Valid, Verdict.Valid)),
            NoDeviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert
            .That(
                report.Failures.Any(f =>
                    f.Contains("c1", StringComparison.Ordinal) && f.Contains("SchemaVerdict", StringComparison.Ordinal)
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task Run_IssuePositionDifferenceWithoutEntry_Fails(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await RunAsync(
            [Case("c1", instance: Verdict.Invalid)],
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 5))),
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 6))),
            NoDeviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("c1").And.Contains("IssuePositions");
    }

    [Test]
    public async Task Run_PositionsAreNotComparedWhenVerdictsDiffer(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = await RunAsync(
            [Case("c1")],
            Engine(Result(Verdict.Valid, Verdict.Valid)),
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 5))),
            NoDeviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("InstanceVerdict");
    }

    [Test]
    public async Task Run_ListedDifference_Passes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviations = new DeviationList(
            "10.0",
            [Entry("c1", DifferenceKind.InstanceVerdict, "instance verdict: ours=Valid, baseline=Invalid")]
        );

        var report = await RunAsync(
            [Case("c1")],
            Engine(Result(Verdict.Valid, Verdict.Valid)),
            Engine(Result(Verdict.Valid, Verdict.Invalid)),
            deviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).IsEmpty();
    }

    [Test]
    public async Task Run_EntryWithOtherObservedText_FailsAsUnlistedAndAsStale(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviations = new DeviationList(
            "10.0",
            [Entry("c1", DifferenceKind.IssuePositions, "issue positions: ours=[3:5], baseline=[3:7]")]
        );

        var report = await RunAsync(
            [Case("c1", instance: Verdict.Invalid)],
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 5))),
            Engine(Result(Verdict.Valid, Verdict.Invalid, new IssuePosition(3, 6))),
            deviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures.Count).IsEqualTo(2);
        await Assert
            .That(
                report.Failures.Any(f =>
                    f.Contains("Unlisted", StringComparison.Ordinal) && f.Contains("c1", StringComparison.Ordinal)
                )
            )
            .IsTrue();
        await Assert
            .That(
                report.Failures.Any(f =>
                    f.Contains("Stale", StringComparison.Ordinal) && f.Contains("c1", StringComparison.Ordinal)
                )
            )
            .IsTrue();
    }

    [Test]
    public async Task Run_StaleEntry_FailsAndNamesTheCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviations = new DeviationList(
            "10.0",
            [Entry("set/group/c1", DifferenceKind.InstanceVerdict, "instance verdict: ours=Valid, baseline=Invalid")]
        );
        var result = Result(Verdict.Valid, Verdict.Valid);

        var report = await RunAsync(
            [Case("set/group/c1")],
            Engine(result),
            Engine(result),
            deviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("Stale").And.Contains("set/group/c1");
    }

    [Test]
    public async Task Run_EntryOfACaseOutsideTheRun_IsNeitherMatchedNorStale(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deviations = new DeviationList(
            "10.0",
            [Entry("other/case", DifferenceKind.SchemaVerdict, "schema verdict: ours=Valid, baseline=Invalid")]
        );
        var result = Result(Verdict.Valid, Verdict.Valid);

        var report = await RunAsync(
            [Case("c1")],
            Engine(result),
            Engine(result),
            deviations,
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).IsEmpty();
    }

    [Test]
    public async Task Run_OnAnotherLeg_SkipsTheBaselineAndAssertsTheRecordedExpectations(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = Engine(Result(Verdict.Invalid, Verdict.NotRun));
        var staleOnThisLeg = new DeviationList(
            "10.0",
            [Entry("c1", DifferenceKind.SchemaVerdict, "schema verdict: ours=Valid, baseline=Invalid")]
        );

        var report = await RunAsync(
            [Case("c1", instance: Verdict.Invalid), Case("c2", instance: Verdict.NotRun)],
            new FakeDifferentialEngine(c =>
                Result(
                    Verdict.Valid,
                    string.Equals(c.Id, "c1", StringComparison.Ordinal) ? Verdict.Invalid : Verdict.NotRun
                )
            ),
            baseline,
            staleOnThisLeg,
            OtherEnvironment,
            cancellationToken
        );

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.BaselineRun).IsFalse();
        await Assert.That(baseline.Calls).IsEmpty();
    }

    [Test]
    public async Task Run_OnAnotherLeg_OurVerdictAgainstTheRecordedExpectation_FailsAndNamesTheCase(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = Engine(Result(Verdict.Valid, Verdict.Valid));

        var report = await RunAsync(
            [Case("set/group/c1")],
            Engine(Result(Verdict.Valid, Verdict.Invalid)),
            baseline,
            NoDeviations,
            OtherEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("set/group/c1").And.Contains("instance");
        await Assert.That(baseline.Calls).IsEmpty();
    }

    [Test]
    public async Task Run_BaselineRuntimeOfTheListDiffersFromTheRunningRuntime_Fails(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Result(Verdict.Valid, Verdict.Valid);

        var report = await RunAsync(
            [Case("c1")],
            Engine(result),
            Engine(result),
            new DeviationList("11.0", []),
            BaselineEnvironment,
            cancellationToken
        );

        await Assert.That(report.Failures).HasSingleItem();
        await Assert.That(report.Failures[0]).Contains("11.0").And.Contains("10.0");
    }
}
