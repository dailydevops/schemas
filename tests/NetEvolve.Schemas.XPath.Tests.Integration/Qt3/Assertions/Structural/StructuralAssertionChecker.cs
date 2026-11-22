namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// Checks assert-deep-eq, assert-permutation, assert-type and assert-xml. The first three ask the engine, through the seam of the
/// evaluated checker, with the items of the query bound to <c>$result</c>; assert-xml serializes the result in <see cref="AssertXmlChecker"/>.
/// </summary>
/// <remarks>
/// assert-permutation is the expression "same count, and for every item of the result the number of items deep-equal to it is the number
/// of items of the expected sequence deep-equal to it", which is a multiset comparison by <c>fn:deep-equal</c>.
/// </remarks>
internal static class StructuralAssertionChecker
{
    public static AssertionVerdict Check(Qt3Case testCase, IQt3Engine engine)
    {
        var actual = engine.Evaluate(testCase);
        if (actual.Kind == EngineResultKind.NotBound)
        {
            return new(CaseOutcome.Fail, actual.Reason);
        }
        if (actual.Kind == EngineResultKind.Error)
        {
            return new(
                CaseOutcome.Fail,
                $"expected a value but got Q{{{actual.ErrorCode.Namespace}}}{actual.ErrorCode.LocalName}"
            );
        }

        var assertion = testCase.Assertion;
        if (string.Equals(assertion.Kind, "assert-xml", StringComparison.Ordinal))
        {
            return AssertXmlChecker.Check(testCase, engine, actual);
        }

        var expected = assertion.Value;
        var query = assertion.Kind switch
        {
            "assert-deep-eq" => $"deep-equal($result, ({expected}))",
            "assert-permutation" =>
                $"count($result) = count(({expected})) and (every $r in $result satisfies count($result[deep-equal(., $r)]) = count(({expected})[deep-equal(., $r)]))",
            _ => $"$result instance of {expected.Trim()}",
        };
        var answer = EvaluatedAssertionChecker.Ask(testCase, engine, actual, query);
        return answer is { Kind: EngineResultKind.Value, Items: [true] }
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, EvaluatedAssertionChecker.Describe(assertion, answer));
    }
}
