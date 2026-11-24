namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

[Explicit]
[Category(ConformanceCategory.Name)]
[ConformanceLeg]
public class Qt3ParserOnlyTests
{
    // The assertion kinds that expect a value; any other kind may expect an error or is a compound assertion.
    private static readonly string[] PositiveKinds =
    [
        "assert",
        "assert-eq",
        "assert-deep-eq",
        "assert-true",
        "assert-false",
        "assert-count",
        "assert-empty",
        "assert-string-value",
        "assert-type",
        "assert-permutation",
        "assert-xml",
    ];

    private static readonly ParserOnlyEngine Engine = new();

    [Test]
    public async Task SelectedCases_ExpectingXpst0003_AllPass(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var failures = new List<string>();
        var count = 0;
        foreach (var testCase in selection.Selected)
        {
            if (testCase.Assertion is { Kind: "error", ErrorCode: "XPST0003" })
            {
                count++;
                var result = Qt3Runner.Run(testCase, Engine);
                if (result.Outcome != CaseOutcome.Pass)
                {
                    failures.Add($"{testCase.Id}: {testCase.Query} ({result.Reason})");
                }
            }
        }

        await Assert.That(failures).IsEmpty();
        await Assert.That(count).IsGreaterThan(0);
    }

    [Test]
    public async Task SelectedCases_ExpectingAValue_NeverReportXpst0003(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var failures = new List<string>();
        var count = 0;
        foreach (var testCase in selection.Selected)
        {
            if (Array.IndexOf(PositiveKinds, testCase.Assertion.Kind) >= 0)
            {
                count++;
                if (Engine.Evaluate(testCase).Kind == EngineResultKind.Error)
                {
                    failures.Add($"{testCase.Id}: {testCase.Query}");
                }
            }
        }

        await Assert.That(failures).IsEmpty();
        await Assert.That(count).IsGreaterThan(0);
    }
}
