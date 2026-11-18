namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

public class GoldenGuardTests
{
    private static readonly GoldenDocumentSource ValidSource = new("suite", "docs/a.xml");

    private static readonly FakeGoldenValidator ValidValidator = new(_ => new GoldenObservation(true, []));

    [Test]
    public async Task EnsurePinned_NullSuiteKey_ThrowsArgumentNullExceptionNamingTheSuiteKey()
    {
        using var fixture = new GoldenFixture();
        var source = ValidSource with { SuiteKey = null! };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            fixture.Corpus.EnsurePinned(source);
            return Task.CompletedTask;
        });

        await Assert.That(ex!.ParamName).Contains("SuiteKey");
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task EnsurePinned_SuiteKeyWithoutText_ThrowsArgumentException(string suiteKey)
    {
        using var fixture = new GoldenFixture();
        var source = ValidSource with { SuiteKey = suiteKey };

        await Assert.That(() => fixture.Corpus.EnsurePinned(source)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_NullSource_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new GoldenFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await fixture.Corpus.ResolveAsync(null!, cancellationToken)
        );
    }

    [Test]
    public async Task Source_NullSuiteKey_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenDocumentSource(null!, "docs/a.xml")).Throws<ArgumentNullException>();

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Source_SuiteKeyWithoutText_ThrowsArgumentException(string suiteKey) =>
        await Assert.That(() => _ = new GoldenDocumentSource(suiteKey, "docs/a.xml")).Throws<ArgumentException>();

    [Test]
    public async Task Source_NullPath_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenDocumentSource("suite", null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Expectation_NullSource_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenExpectation(null!, [], true)).Throws<ArgumentNullException>();

    [Test]
    public async Task Expectation_NullRuleIds_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenExpectation(ValidSource, null!, true)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Expectation_RuleIdWithoutText_ThrowsArgumentException(string ruleId) =>
        await Assert.That(() => _ = new GoldenExpectation(ValidSource, [ruleId], true)).Throws<ArgumentException>();

    [Test]
    public async Task RuleSet_NullName_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenRuleSet(null!, ValidValidator, [])).Throws<ArgumentNullException>();

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task RuleSet_NameWithoutText_ThrowsArgumentException(string name) =>
        await Assert.That(() => _ = new GoldenRuleSet(name, ValidValidator, [])).Throws<ArgumentException>();

    [Test]
    public async Task RuleSet_NullValidator_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenRuleSet("rules", null!, [])).Throws<ArgumentNullException>();

    [Test]
    public async Task RuleSet_NullExpectations_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenRuleSet("rules", ValidValidator, null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task RuleSet_NullExpectation_ThrowsArgumentNullException() =>
        await Assert
            .That(() => _ = new GoldenRuleSet("rules", ValidValidator, [null!]))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Observation_NullFiredRuleIds_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new GoldenObservation(true, null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments(null, "path")]
    [Arguments("", "path")]
    [Arguments("   ", "path")]
    [Arguments("id", null)]
    [Arguments("id", "")]
    [Arguments("id", "   ")]
    public async Task Document_IdOrPathWithoutText_ThrowsArgumentException(string? id, string? path) =>
        await Assert.That(() => _ = new GoldenDocument(id!, path!)).Throws<ArgumentException>();

    [Test]
    public async Task FakeValidator_NullDocument_ThrowsArgumentNullException(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ValidValidator.ValidateAsync(null!, cancellationToken)
        );
    }

    [Test]
    public async Task FakeValidator_NullVerdict_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = new FakeGoldenValidator(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Fixture_SourceWithNullName_ThrowsArgumentNullException() =>
        await Assert.That(() => _ = GoldenFixture.Source(null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Fixture_SourceWithoutName_ThrowsArgumentException(string name) =>
        await Assert.That(() => _ = GoldenFixture.Source(name)).Throws<ArgumentException>();
}
