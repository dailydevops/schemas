namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>The pinned libxml2 regexp and error sub-trees run on the regex engine and the reader; run on demand with the conformance category.</summary>
public class Libxml2ConformanceTests
{
    private const string RegexpBaselines = "libxml2-regexp";
    private const string ErrorsBaselines = "libxml2-errors";

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Suite_HoldsThePinnedCaseCounts(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That((await RunRegexpAsync(cancellationToken)).Count).IsEqualTo(157);
        await Assert.That((await RunErrorsAsync(cancellationToken)).Count).IsEqualTo(45);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Regexp_Baseline_MatchesTheRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = Libxml2BaselineSet.Compare(
            Libxml2Suite.BaselineDirectory(RegexpBaselines),
            await RunRegexpAsync(cancellationToken)
        );

        await Assert.That(report.IsSuccess).IsTrue().Because(Libxml2BaselineSet.Describe(report));
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Errors_Baseline_MatchesTheRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var report = Libxml2BaselineSet.Compare(
            Libxml2Suite.BaselineDirectory(ErrorsBaselines),
            await RunErrorsAsync(cancellationToken)
        );

        await Assert.That(report.IsSuccess).IsTrue().Because(Libxml2BaselineSet.Describe(report));
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
    }

    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var regexp = await RunRegexpAsync(cancellationToken);
        var errors = await RunErrorsAsync(cancellationToken);

        Libxml2BaselineSet.Regenerate(Libxml2Suite.BaselineDirectory(RegexpBaselines), regexp);
        Libxml2BaselineSet.Regenerate(Libxml2Suite.BaselineDirectory(ErrorsBaselines), errors);

        await Assert.That(regexp).IsNotEmpty();
        await Assert.That(errors).IsNotEmpty();
    }

    private static async Task<List<CaseResult>> RunRegexpAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Libxml2Suite.FetchAsync(cancellationToken);
        return Libxml2RegexRunner.Run(Libxml2RegexCatalog.Read(root), cancellationToken);
    }

    private static async Task<List<CaseResult>> RunErrorsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await Libxml2Suite.FetchAsync(cancellationToken);
        return await Libxml2ErrorRunner.RunAsync(Libxml2ErrorCatalog.Read(root), cancellationToken);
    }
}
