namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// Checks assert, assert-eq, assert-count, and assert-string-value. The engine evaluates the query first; an assertion that needs an
/// expression asks the engine through the same seam, with the items of the query bound to <c>$result</c>.
/// </summary>
internal static class EvaluatedAssertionChecker
{
    // The assertion definition of the catalog: string-join(for $r in $result return string($r), " ").
    private const string StringValueQuery = "string-join(for $r in $result return string($r), ' ')";

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
        return assertion.Kind switch
        {
            "assert" => CheckBoolean(testCase, engine, actual, $"boolean(({assertion.Value}))"),
            "assert-eq" => CheckBoolean(testCase, engine, actual, $"boolean($result eq ({assertion.Value}))"),
            "assert-count" => CheckCount(assertion, actual),
            _ => CheckStringValue(testCase, engine, actual),
        };
    }

    private static AssertionVerdict CheckCount(Qt3Assertion assertion, EngineResult actual) =>
        long.TryParse(
            assertion.Value.Trim(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out var count
        )
        && count == actual.Items.Count
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, $"expected {assertion.Value.Trim()} items but got {actual.Items.Count}");

    private static AssertionVerdict CheckBoolean(Qt3Case testCase, IQt3Engine engine, EngineResult actual, string query)
    {
        var answer = Ask(testCase, engine, actual, query);
        return answer is { Kind: EngineResultKind.Value, Items: [true] }
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, Describe(testCase.Assertion, answer));
    }

    private static AssertionVerdict CheckStringValue(Qt3Case testCase, IQt3Engine engine, EngineResult actual)
    {
        var answer = Ask(testCase, engine, actual, StringValueQuery);
        if (answer is not { Kind: EngineResultKind.Value, Items: [string value] })
        {
            return new(CaseOutcome.Fail, Describe(testCase.Assertion, answer));
        }

        var assertion = testCase.Assertion;
        var expected = assertion.Value;
        if (assertion.NormalizeSpace)
        {
            value = NormalizeSpace(value);
            expected = NormalizeSpace(expected);
        }

        return string.Equals(value, expected, StringComparison.Ordinal)
            ? new(CaseOutcome.Pass)
            : new(CaseOutcome.Fail, $"expected string value '{expected}' but got '{value}'");
    }

    private static EngineResult Ask(Qt3Case testCase, IQt3Engine engine, EngineResult actual, string query) =>
        engine.Evaluate(testCase with { Query = query, QueryFile = null, ResultVariable = actual.Items });

    private static string Describe(Qt3Assertion assertion, EngineResult answer) =>
        answer.Kind == EngineResultKind.Error
            ? $"{assertion.Kind}: the expression raised Q{{{answer.ErrorCode.Namespace}}}{answer.ErrorCode.LocalName}"
            : $"{assertion.Kind}: the expression did not hold";

    // The normalize-space() function: strip leading and trailing white space and collapse the runs inside.
    private static string NormalizeSpace(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var c in text)
        {
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                pendingSpace = builder.Length > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    _ = builder.Append(' ');
                    pendingSpace = false;
                }
                _ = builder.Append(c);
            }
        }
        return builder.ToString();
    }
}
