namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Text.Unicode;

/// <summary>
/// The pinned parse-number-fxx test data (Apache-2.0) run against <c>xs:double</c> and <c>xs:float</c> parsing; run on demand with the
/// conformance category. The baseline is empty: any mismatch fails the test.
/// </summary>
public class ParseNumberFxxConformanceTests
{
    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    [Arguments("exhaustive-float16.txt")]
    [Arguments("freetype-2-7.txt")]
    [Arguments("google-double-conversion.txt")]
    [Arguments("google-wuffs.txt")]
    [Arguments("ibm-fpgen.txt")]
    [Arguments("lemire-fast-double-parser.txt")]
    [Arguments("lemire-fast-float.txt")]
    [Arguments("more-test-cases.txt")]
    [Arguments("remyoudompheng-fptest-0.txt")]
    [Arguments("remyoudompheng-fptest-1.txt")]
    [Arguments("remyoudompheng-fptest-2.txt")]
    [Arguments("remyoudompheng-fptest-3.txt")]
    [Arguments("tencent-rapidjson.txt")]
    [Arguments("ulfjack-ryu.txt")]
    public async Task DataFile_ParsesToTheExpectedBits(string file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(UcdData.RepositoryRoot(), "tests", "conformance", "suites", "parse-number-fxx.json");
        var manifest = SuiteManifestLoader.Parse(
            await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false),
            path
        );
        using var http = new HttpClient();
        var fetcher = new SuiteFetcher(new HttpSuiteTransport(http), ConformanceCache.ResolveRoot());
        var root = await fetcher.FetchAsync(manifest, cancellationToken).ConfigureAwait(false);

        var passed = 0;
        var failures = new List<string>();
        foreach (
            var line in await File.ReadAllLinesAsync(Path.Combine(root, file), cancellationToken).ConfigureAwait(false)
        )
        {
            switch (ParseNumberFxx.Check(line, XsdVersion.V10))
            {
                case ParseNumberFxxOutcome.Passed:
                    passed++;
                    break;
                case ParseNumberFxxOutcome.Rejected:
                    break;
                default:
                    failures.Add(line);
                    break;
            }
        }

        await Assert
            .That(failures)
            .IsEmpty()
            .Because($"{failures.Count} lines differ, first: {string.Join(" | ", failures.Take(10))}");
        await Assert.That(passed).IsGreaterThan(0);
    }
}
