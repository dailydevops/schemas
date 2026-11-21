namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>Runs cases against an engine and checks the assertions of the static and the evaluated checker; all others are skipped.</summary>
internal static class Qt3Runner
{
    public static CaseResult Run(Qt3Case testCase, IQt3Engine engine)
    {
        // Cases whose assertion neither checker decides are skipped without calling the engine, so they can never pass.
        var verdict = testCase.Assertion.IsEvaluated
            ? EvaluatedAssertionChecker.Check(testCase, engine)
            : StaticAssertionChecker.Check(
                testCase.Assertion,
                testCase.Assertion.IsActive ? engine.Evaluate(testCase) : EngineResult.NotBound
            );
        return new CaseResult(testCase.Id, testCase.TestSet, verdict.Outcome, verdict.Reason);
    }

    public static List<CaseResult> Run(IEnumerable<Qt3Case> cases, IQt3Engine engine)
    {
        var results = new List<CaseResult>();
        foreach (var testCase in cases)
        {
            results.Add(Run(testCase, engine));
        }
        return results;
    }
}
