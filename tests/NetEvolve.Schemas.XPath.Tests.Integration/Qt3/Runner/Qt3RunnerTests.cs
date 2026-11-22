namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using static Qt3Fixtures;

public class Qt3RunnerTests
{
    private const string ErrNs = "http://www.w3.org/2005/xqt-errors";

    private static CaseResult Run(Qt3Assertion assertion, EngineResult result) =>
        Qt3Runner.Run(Case(assertion), new FakeEngine(_ => result));

    private static Qt3Assertion Active(string kind) => new(kind);

    private static Qt3Assertion Error(string code) => new("error", code);

    [Test]
    [Arguments("assert-true", "")]
    [Arguments("assert-eq", "1")]
    public async Task Run_EngineResultNotBound_ReportsItsReason(string kind, string value)
    {
        var result = Run(new Qt3Assertion(kind, Value: value), EngineResult.NotBoundBecause("a limit was hit"));

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsEqualTo("a limit was hit");
    }

    [Test]
    public async Task Run_UnboundEngine_ReportsThatNoEngineIsBound() =>
        await Assert
            .That(Qt3Runner.Run(Case(Active("assert-true")), new UnboundEngine()).Reason)
            .IsEqualTo("no engine is bound");

    [Test]
    public async Task Run_AssertTrue_PassesOnSingletonTrueAndFailsOtherwise()
    {
        var assertion = Active("assert-true");

        await Assert.That(Run(assertion, EngineResult.Value(true)).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(assertion, EngineResult.Value(false)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value(true, true)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value()).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value("true")).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Error(new QName(ErrNs, "FOER0000"))).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_AssertFalse_PassesOnSingletonFalseAndFailsOtherwise()
    {
        var assertion = Active("assert-false");

        await Assert.That(Run(assertion, EngineResult.Value(false)).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(assertion, EngineResult.Value(true)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value(false, false)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value()).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(assertion, EngineResult.Value(0)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Error(new QName(ErrNs, "FOER0000"))).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_AssertEmpty_PassesOnEmptySequenceAndFailsOtherwise()
    {
        var assertion = Active("assert-empty");

        await Assert.That(Run(assertion, EngineResult.Value()).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(assertion, EngineResult.Value(1)).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Error(new QName(ErrNs, "FOER0000"))).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_Error_ComparesExpectedCodeAsQName()
    {
        var actual = EngineResult.Error(new QName(ErrNs, "XPST0003"));

        await Assert.That(Run(Error("XPST0003"), actual).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(Error("XPST0017"), actual).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(Error("*"), actual).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(Error($"Q{{{ErrNs}}}XPST0003"), actual).Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Run(Error("Q{urn:other}XPST0003"), actual).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(Error("XPST0003"), EngineResult.Error(new QName("urn:other", "XPST0003"))).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(Error("Q{}USER9999"), EngineResult.Error(new QName("", "USER9999"))).Outcome)
            .IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_Error_FailsWhenEngineReturnsValue()
    {
        await Assert.That(Run(Error("XPST0003"), EngineResult.Value()).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Run(Error("*"), EngineResult.Value(true)).Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    [Arguments("serialization-matches")]
    [Arguments("assert-serialization-error")]
    public async Task Run_OtherAssertionKind_IsSkippedNeverPassed(string kind)
    {
        var engine = new FakeEngine(_ => EngineResult.Value(true));

        var result = Qt3Runner.Run(Case(Active(kind)), engine);

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Skipped);
        await Assert.That(result.Reason).Contains(kind);
        await Assert.That(engine.Calls).IsEqualTo(0);
    }

    [Test]
    [Arguments("any-of")]
    [Arguments("all-of")]
    [Arguments("not")]
    public async Task Run_AnyOfWrappingActiveKinds_IsSkippedNeverPassed(string compound)
    {
        using var temp = new TempDirectory();
        var root = WriteSuite(
            temp,
            ("set", TestCase("c1", result: $"<{compound}><assert-true/><error code=\"*\"/></{compound}>"))
        );
        var testCase = Qt3CatalogReader.Read(root)[0].Cases[0];
        var engine = new FakeEngine(_ => EngineResult.Value(true));

        var result = Qt3Runner.Run(testCase, engine);

        await Assert.That(testCase.Assertion.Kind).IsEqualTo(compound);
        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Skipped);
    }

    [Test]
    public async Task Run_UnboundEngine_FailsActiveCases()
    {
        var result = Qt3Runner.Run(Case(Error("*")), new UnboundEngine());

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_Cases_RecordsIdAndTestSetPerCase()
    {
        var cases = new[] { Case(Active("assert-empty"), "a", "s1"), Case(Active("assert-empty"), "b", "s2") };

        var results = Qt3Runner.Run(cases, new FakeEngine(_ => EngineResult.Value()));

        await Assert.That(results[0]).IsEqualTo(new CaseResult("s1/a", "s1", CaseOutcome.Pass));
        await Assert.That(results[1]).IsEqualTo(new CaseResult("s2/b", "s2", CaseOutcome.Pass));
    }

    [Test]
    public async Task FailureMessage_ListsFirstTwentyIdsAndTotal()
    {
        var regressions = new List<string>();
        for (var i = 0; i < 25; i++)
        {
            regressions.Add($"set/case-{i:D2}");
        }
        var report = new TestSetReport("set", new TestSetCounts(0, 25, 0), regressions, ["set/lucky"]);

        var message = Qt3FailureMessage.Format(report);

        await Assert.That(message).Contains("25 regression(s)");
        await Assert.That(message).Contains("set/case-19");
        await Assert.That(message).DoesNotContain("set/case-20");
        await Assert.That(message).Contains("1 unexpected pass(es)");
        await Assert.That(message).Contains("set/lucky");
    }
}
