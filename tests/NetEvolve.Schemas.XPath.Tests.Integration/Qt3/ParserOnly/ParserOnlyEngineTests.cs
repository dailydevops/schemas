namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class ParserOnlyEngineTests
{
    private static readonly ParserOnlyEngine Engine = new();

    private static Qt3Case Case(string query, string? queryFile = null, string code = "XPST0003") =>
        new("set", "c1", query, queryFile, null, new Qt3Assertion("error", code), []);

    [Test]
    public async Task Run_SyntaxErrorWhereXpst0003IsExpected_Passes()
    {
        var result = Qt3Runner.Run(Case("1 +"), Engine);

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_ValidQueryWhereXpst0003IsExpected_FailsBecauseNothingIsEvaluated()
    {
        var result = Qt3Runner.Run(Case("1 + 2"), Engine);

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_SyntaxErrorWhereAnotherCodeIsExpected_Fails()
    {
        var result = Qt3Runner.Run(Case("1 +", code: "XPST0008"), Engine);

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_QueryInAFile_ReadsTheFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var file = Path.Combine(directory.Path, "query.xq");
        await File.WriteAllTextAsync(file, "1 +", cancellationToken);

        var result = Qt3Runner.Run(Case(string.Empty, file), Engine);

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Evaluate_ValidQuery_ReportsNoResult()
    {
        var result = Engine.Evaluate(Case("a/b"));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.NotBound);
    }
}
