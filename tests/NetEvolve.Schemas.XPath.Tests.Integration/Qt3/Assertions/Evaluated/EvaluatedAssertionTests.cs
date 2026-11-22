namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

/// <summary>The four evaluated assertion kinds against a fake engine: the query result is the first call, the assertion expression the second.</summary>
public class EvaluatedAssertionTests
{
    private static readonly QName Error = new(QName.ErrorNamespace, "FOER0000");

    /// <summary>Runs one case; the fake answers the query with <paramref name="actual"/> and an assertion expression with <paramref name="expression"/>.</summary>
    private static CaseResult Run(
        Qt3Assertion assertion,
        EngineResult actual,
        EngineResult? expression,
        out List<Qt3Case> calls
    )
    {
        var recorded = new List<Qt3Case>();
        var engine = new FakeEngine(testCase =>
        {
            recorded.Add(testCase);
            return testCase.ResultVariable is null ? actual : expression ?? EngineResult.NotBound;
        });
        var result = Qt3Runner.Run(Case(assertion), engine);
        calls = recorded;
        return result;
    }

    [Test]
    public async Task Assert_ExpressionTrue_PassesAndSeesTheResultVariable()
    {
        var assertion = new Qt3Assertion("assert", Value: "$result = 1");

        var pass = Run(assertion, EngineResult.Value(1), EngineResult.Value(true), out var calls);

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[1].Query).Contains("$result = 1");
        await Assert.That(calls[1].QueryFile).IsNull();
        await Assert.That(calls[1].ResultVariable!.Count).IsEqualTo(1);
        await Assert.That(calls[1].ResultVariable![0]).IsEqualTo(1);
    }

    [Test]
    public async Task Assert_ExpressionFalseOrError_Fails()
    {
        var assertion = new Qt3Assertion("assert", Value: "$result = 1");

        await Assert
            .That(Run(assertion, EngineResult.Value(2), EngineResult.Value(false), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Value(2), EngineResult.Error(Error), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Value(2), EngineResult.Value(), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task AssertEq_ExpressionEqualsTheResult_Passes()
    {
        var assertion = new Qt3Assertion("assert-eq", Value: "1.5e0");

        var pass = Run(assertion, EngineResult.Value(1.5), EngineResult.Value(true), out var calls);

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(calls[1].Query).Contains("$result");
        await Assert.That(calls[1].Query).Contains("1.5e0");
    }

    [Test]
    public async Task AssertEq_ExpressionDiffersOrComparisonRaisesAnError_Fails()
    {
        var assertion = new Qt3Assertion("assert-eq", Value: "1.5e0");

        await Assert
            .That(Run(assertion, EngineResult.Value(2.5), EngineResult.Value(false), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Value(1, 2), EngineResult.Error(Error), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task AssertCount_ResultHasTheCount_PassesWithoutAskingTheEngineAgain()
    {
        var assertion = new Qt3Assertion("assert-count", Value: "3");

        var pass = Run(assertion, EngineResult.Value(1, 2, 3), null, out var calls);

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(calls.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("2")]
    [Arguments("not a number")]
    public async Task AssertCount_WrongOrMalformedCount_Fails(string expected)
    {
        var assertion = new Qt3Assertion("assert-count", Value: expected);

        await Assert.That(Run(assertion, EngineResult.Value(1, 2, 3), null, out _).Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task AssertStringValue_StringValueMatches_Passes()
    {
        var assertion = new Qt3Assertion("assert-string-value", Value: "a b");

        var pass = Run(assertion, EngineResult.Value("a", "b"), EngineResult.Value("a b"), out var calls);

        await Assert.That(pass.Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(calls[1].Query).Contains("string-join");
        await Assert.That(calls[1].ResultVariable!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AssertStringValue_StringValueDiffersOrEngineFails_Fails()
    {
        var assertion = new Qt3Assertion("assert-string-value", Value: "a b");

        await Assert
            .That(Run(assertion, EngineResult.Value("a", "c"), EngineResult.Value("a c"), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(assertion, EngineResult.Value("a"), EngineResult.Error(Error), out _).Outcome)
            .IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task AssertStringValue_NormalizeSpace_NormalizesBothSides()
    {
        var expected = " a   b ";
        var actual = EngineResult.Value("\ta \n b");
        var plain = new Qt3Assertion("assert-string-value", Value: expected);
        var normalized = plain with { NormalizeSpace = true };

        await Assert.That(Run(plain, EngineResult.Value("a b"), actual, out _).Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert
            .That(Run(normalized, EngineResult.Value("a b"), actual, out _).Outcome)
            .IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    [Arguments("assert", "true()")]
    [Arguments("assert-eq", "1")]
    [Arguments("assert-count", "1")]
    [Arguments("assert-string-value", "x")]
    public async Task EveryKind_QueryRaisesAnError_FailsWithoutEvaluatingTheAssertion(string kind, string value)
    {
        var result = Run(
            new Qt3Assertion(kind, Value: value),
            EngineResult.Error(Error),
            EngineResult.Value(true),
            out var calls
        );

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(calls.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("assert", "true()")]
    [Arguments("assert-eq", "1")]
    [Arguments("assert-count", "1")]
    [Arguments("assert-string-value", "x")]
    public async Task EveryKind_EngineNotBound_Fails(string kind, string value)
    {
        var result = Qt3Runner.Run(Case(new Qt3Assertion(kind, Value: value)), new UnboundEngine());

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }
}
