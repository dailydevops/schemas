namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Checks the assertion kinds that need no evaluator: error, assert-true, assert-false, assert-empty.</summary>
internal static class StaticAssertionChecker
{
    public static AssertionVerdict Check(Qt3Assertion assertion, EngineResult result)
    {
        if (!assertion.IsActive)
        {
            return new(CaseOutcome.Skipped, $"assertion '{assertion.Kind}' is not checked by the static runner");
        }
        if (result.Kind == EngineResultKind.NotBound)
        {
            return new(CaseOutcome.Fail, result.Reason);
        }

        return assertion.Kind switch
        {
            "error" => CheckError(assertion.ErrorCode ?? string.Empty, result),
            "assert-true" => CheckBoolean(true, result),
            "assert-false" => CheckBoolean(false, result),
            _ => CheckEmpty(result),
        };
    }

    private static AssertionVerdict CheckError(string expected, EngineResult result)
    {
        if (result.Kind != EngineResultKind.Error)
        {
            return new(CaseOutcome.Fail, $"expected error '{expected}' but the query returned a value");
        }

        return
            string.Equals(expected, "*", StringComparison.Ordinal) || QName.ParseErrorCode(expected) == result.ErrorCode
            ? new(CaseOutcome.Pass)
            : new(
                CaseOutcome.Fail,
                $"expected error '{expected}' but got Q{{{result.ErrorCode.Namespace}}}{result.ErrorCode.LocalName}"
            );
    }

    private static AssertionVerdict CheckBoolean(bool expected, EngineResult result) =>
        result.Kind == EngineResultKind.Value && result.Items is [bool actual] && actual == expected
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, $"expected a singleton xs:boolean {(expected ? "true" : "false")}");

    private static AssertionVerdict CheckEmpty(EngineResult result) =>
        result.Kind == EngineResultKind.Value && result.Items.Count == 0
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, "expected the empty sequence");
}
