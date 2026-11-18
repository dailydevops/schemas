namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;

public class GoldenHarnessTests
{
    private static GoldenExpectation Expect(string document, bool valid, params string[] rules) =>
        new(GoldenFixture.Source(document), rules, valid);

    private static GoldenRuleSet RuleSet(
        string name,
        IGoldenValidator validator,
        params GoldenExpectation[] expected
    ) => new(name, validator, expected);

    private static async Task<IReadOnlyList<CaseResult>> RunAsync(
        GoldenFixture fixture,
        GoldenRuleSet ruleSet,
        CancellationToken cancellationToken
    ) => await GoldenHarness.RunAsync(fixture.Corpus, [ruleSet], cancellationToken);

    [Test]
    public async Task Run_FiredRulesEqualExpectedRules_IsPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(false, ["BR-02", "BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", false, "BR-01", "BR-02")),
            cancellationToken
        );

        var result = results.Single();
        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(result.CaseId).IsEqualTo("golden-rules/docs/a.xml");
        await Assert.That(result.TestSet).IsEqualTo("rules-a");
        await Assert.That(validator.Calls).IsEquivalentTo(["golden-rules/docs/a.xml"]);
    }

    [Test]
    public async Task Run_ValidatorReceivesTheFetchedDocument(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var content = string.Empty;
        var validator = new FakeGoldenValidator(d =>
        {
            content = File.ReadAllText(d.Path);
            return new GoldenObservation(true, []);
        });

        _ = await RunAsync(fixture, RuleSet("rules-a", validator, Expect("a", true)), cancellationToken);

        await Assert.That(content).IsEqualTo("<doc/>");
    }

    [Test]
    public async Task Run_MissingExpectedRule_FailsNamingTheRuleId(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(false, ["BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", false, "BR-01", "BR-02")),
            cancellationToken
        );

        var result = results.Single();
        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsEqualTo("Missing expected rule 'BR-02'.");
    }

    [Test]
    public async Task Run_UnexpectedFiredRule_FailsNamingTheRuleId(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(false, ["BR-01", "BR-99"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", false, "BR-01")),
            cancellationToken
        );

        var result = results.Single();
        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsEqualTo("Unexpected fired rule 'BR-99'.");
    }

    [Test]
    public async Task Run_MissingAndUnexpectedRules_AreReportedSortedAndTogether(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, ["Z-1", "Y-1"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", true, "B-2", "A-1")),
            cancellationToken
        );

        await Assert
            .That(results.Single().Reason)
            .IsEqualTo(
                "Missing expected rule 'A-1'. Missing expected rule 'B-2'. Unexpected fired rule 'Y-1'. Unexpected fired rule 'Z-1'."
            );
    }

    [Test]
    public async Task Run_RuleFiresForSeveralContextNodes_IsComparedAsOneRule(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, ["BR-01", "BR-01", "BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", true, "BR-01")),
            cancellationToken
        );

        await Assert.That(results.Single().Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_FiredRuleWithWhitespaceId_IsIgnored(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, ["  ", "BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", true, "BR-01")),
            cancellationToken
        );

        await Assert.That(results.Single().Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_FiredRuleWithoutId_IsIgnored(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, ["", "BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", true, "BR-01")),
            cancellationToken
        );

        await Assert.That(results.Single().Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_ValidityDiffers_Fails(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, ["BR-01"]));

        var results = await RunAsync(
            fixture,
            RuleSet("rules-a", validator, Expect("a", false, "BR-01")),
            cancellationToken
        );

        var result = results.Single();
        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsEqualTo("Expected invalid but observed valid.");
    }

    [Test]
    public async Task Run_SourceWithoutPinnedChecksum_IsRejectedBeforeAnyValidation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));
        var unpinned = new GoldenExpectation(new GoldenDocumentSource("unpinned-suite", "docs/a.xml"), [], true);

        var ex = await Assert.ThrowsAsync<GoldenSourceException>(async () =>
            await RunAsync(fixture, RuleSet("rules-a", validator, Expect("a", true), unpinned), cancellationToken)
        );

        await Assert.That(ex!.Message).Contains("unpinned-suite");
        await Assert.That(validator.Calls).IsEmpty();
        await Assert.That(fixture.Transport.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Run_PassRatePerRuleSet_IsPassedOverTotal(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var failing = new FakeGoldenValidator(d => new GoldenObservation(
            true,
            d.Id.EndsWith("d.xml", StringComparison.Ordinal) ? ["OTHER"] : ["BR-01"]
        ));
        var passing = new FakeGoldenValidator(_ => new GoldenObservation(true, ["BR-01"]));
        var ruleSets = new[]
        {
            RuleSet(
                "rules-a",
                failing,
                Expect("a", true, "BR-01"),
                Expect("b", true, "BR-01"),
                Expect("c", true, "BR-01"),
                Expect("d", true, "BR-01")
            ),
            RuleSet("rules-b", passing, Expect("a", true, "BR-01"), Expect("b", true, "BR-01")),
        };

        var results = await GoldenHarness.RunAsync(fixture.Corpus, ruleSets, cancellationToken);
        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
        );

        await Assert.That(report.TestSets.Select(s => s.TestSet)).IsEquivalentTo(["rules-a", "rules-b"]);
        await Assert.That(report.TestSets[0].Counts).IsEqualTo(new TestSetCounts(3, 1, 0));
        await Assert.That(report.TestSets[0].Counts.PassRate).IsEqualTo(3d / 4d);
        await Assert.That(report.TestSets[1].Counts.PassRate).IsEqualTo(1d);
    }

    [Test]
    public async Task Run_SameDocumentInTwoRuleSets_IsReportedPerRuleSet(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var passing = new FakeGoldenValidator(_ => new GoldenObservation(true, ["BR-01"]));
        var failing = new FakeGoldenValidator(_ => new GoldenObservation(true, []));

        var results = await GoldenHarness.RunAsync(
            fixture.Corpus,
            [
                RuleSet("rules-a", passing, Expect("a", true, "BR-01")),
                RuleSet("rules-b", failing, Expect("a", true, "BR-01")),
            ],
            cancellationToken
        );

        await Assert
            .That(
                results.Count(r =>
                    r.Outcome == CaseOutcome.Pass && string.Equals(r.TestSet, "rules-a", StringComparison.Ordinal)
                )
            )
            .IsEqualTo(1);
        await Assert
            .That(
                results.Count(r =>
                    r.Outcome == CaseOutcome.Fail && string.Equals(r.TestSet, "rules-b", StringComparison.Ordinal)
                )
            )
            .IsEqualTo(1);
        await Assert.That(fixture.Transport.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task Run_NullArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await GoldenHarness.RunAsync(null!, [], cancellationToken)
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await GoldenHarness.RunAsync(fixture.Corpus, null!, cancellationToken)
        );
    }

    [Test]
    public async Task Run_ExpectedRuleIdWithoutText_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));

        var blankId = Expect("a", true) with { ExpectedRuleIds = [" "] };

        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await RunAsync(fixture, RuleSet("rules-a", validator, blankId), cancellationToken)
        );
    }

    [Test]
    public async Task Run_NullRuleSet_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await GoldenHarness.RunAsync(fixture.Corpus, [null!], cancellationToken)
        );
    }

    [Test]
    public async Task Run_NullExpectationEntry_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));
        var ruleSet = RuleSet("rules-a", validator) with { Expectations = [null!] };

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, ruleSet, cancellationToken)
        );
    }

    [Test]
    public async Task Run_RuleSetWithNullMember_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));
        var valid = RuleSet("rules-a", validator, Expect("a", true));

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, valid with { Name = null! }, cancellationToken)
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, valid with { Validator = null! }, cancellationToken)
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, valid with { Expectations = null! }, cancellationToken)
        );
    }

    [Test]
    public async Task Run_BlankRuleSetName_ThrowsArgumentException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));
        var blank = RuleSet("rules-a", validator, Expect("a", true)) with { Name = " " };

        _ = await Assert.ThrowsAsync<ArgumentException>(async () => await RunAsync(fixture, blank, cancellationToken));
    }

    [Test]
    public async Task Run_ExpectationWithNullMember_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []));

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(
                fixture,
                RuleSet("rules-a", validator, Expect("a", true) with { Source = null! }),
                cancellationToken
            )
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(
                fixture,
                RuleSet("rules-a", validator, Expect("a", true) with { ExpectedRuleIds = null! }),
                cancellationToken
            )
        );
    }

    [Test]
    public async Task Run_NullObservation_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => null!);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, RuleSet("rules-a", validator, Expect("a", true)), cancellationToken)
        );
    }

    [Test]
    public async Task Run_ObservationWithNullFiredRuleIds_ThrowsArgumentNullException(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();
        var validator = new FakeGoldenValidator(_ => new GoldenObservation(true, []) with { FiredRuleIds = null! });

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await RunAsync(fixture, RuleSet("rules-a", validator, Expect("a", true)), cancellationToken)
        );
    }
}
