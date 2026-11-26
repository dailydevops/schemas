namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

public class XIncludeSuiteTests
{
    [Test]
    public async Task Manifest_PinsTheArchiveByChecksum()
    {
        var manifest = SuitePinFiles.Load("xinclude.json");

        await Assert.That(manifest.Key).IsEqualTo("xinclude");
        await Assert.That(manifest.SourceUrl.Scheme).IsEqualTo(Uri.UriSchemeHttps);
        await Assert.That(manifest.SourceUrl.AbsolutePath).EndsWith("/XIncl20060927.zip");
        await Assert.That(manifest.Sha256.Length).IsEqualTo(64);
    }

    [Test]
    public async Task CatalogReader_ReadsContributorPathsFeaturesAndType()
    {
        using var suite = new XIncludeFixtureSuite();

        var cases = XIncludeCatalogReader.Read(suite.Root);
        var first = cases[0];
        var last = cases[^1];

        await Assert.That(cases.Count).IsEqualTo(12);
        await Assert.That(first.Id).IsEqualTo("alpha-ok");
        await Assert.That(first.Contributor).IsEqualTo("Alpha");
        await Assert.That(first.TestSet).IsEqualTo("Alpha");
        await Assert.That(first.InputPath).IsEqualTo("Alpha/test/docs/ok.xml");
        await Assert.That(first.OutputPath).IsEqualTo("Alpha/result/ok.xml");
        await Assert.That(first.Type).IsEqualTo(XIncludeCaseType.Success);
        await Assert.That(first.Description).IsEqualTo("Includes a document.");
        await Assert.That(first.Features).IsEmpty();
        await Assert.That(cases[3].Type).IsEqualTo(XIncludeCaseType.Error);
        await Assert.That(cases[3].OutputPath).IsNull();
        await Assert.That(last.OutputPath).IsEqualTo("Beta/result/ok.xml");
        await Assert.That(last.Features).IsEquivalentTo(["unparsed-entities", "lang-fixup"]);
    }

    [Test]
    [Arguments(
        "<testsuite><testcases basedir=\"A/t\"><testcase id=\"x\" href=\"a.xml\" type=\"maybe\"/></testcases></testsuite>",
        "unknown type"
    )]
    [Arguments(
        "<testsuite><testcases basedir=\"A/t\"><testcase href=\"a.xml\" type=\"error\"/></testcases></testsuite>",
        "no 'id'"
    )]
    [Arguments(
        "<testsuite><testcases basedir=\"A/t\"><testcase id=\"x\" href=\"a.xml\" type=\"error\"/><testcase id=\"x\" href=\"b.xml\" type=\"error\"/></testcases></testsuite>",
        "listed twice"
    )]
    public async Task CatalogReader_InvalidCatalog_Throws(
        string catalog,
        string message,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.Combine(Path.GetTempPath(), "xinclude-catalog-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "testdescr.xml"), catalog, cancellationToken);

            var failure = Assert.Throws<InvalidDataException>(() => XIncludeCatalogReader.Read(root));

            await Assert.That(failure.Message).Contains(message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Runner_JudgesEveryKindOfCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new XIncludeFixtureSuite();

        var results = XIncludeRunner.Run(suite.Root, XIncludeCatalogReader.Read(suite.Root), cancellationToken);
        var byId = results.ToDictionary(r => r.CaseId, StringComparer.Ordinal);

        await Assert.That(byId["alpha-ok"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(byId["alpha-differs"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-differs"].Reason).Contains("differs from the reference result");
        await Assert.That(byId["alpha-no-output"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(byId["alpha-missing"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(byId["alpha-error-but-ok"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-error-but-ok"].Reason).Contains("Expected a fatal error");
        await Assert.That(byId["alpha-missing-but-success"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-missing-but-success"].Reason).Contains("Expected success");
        await Assert.That(byId["alpha-malformed-error"].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(byId["alpha-malformed-success"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-malformed-success"].Reason).Contains("not well formed");
        await Assert.That(byId["alpha-no-reference"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-no-reference"].Reason).Contains("cannot be read");
        await Assert.That(byId["alpha-bad-reference"].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(byId["alpha-xpointer"].Outcome).IsEqualTo(CaseOutcome.Skipped);
        await Assert.That(byId["alpha-xpointer"].Reason).Contains("xpointer");
        await Assert.That(byId["beta-entities"].Outcome).IsEqualTo(CaseOutcome.Skipped);
        await Assert.That(byId["beta-entities"].TestSet).IsEqualTo("Beta");
    }

    [Test]
    public async Task Runner_LongMismatch_ShortensTheReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new XIncludeFixtureSuite();
        var wide = "<doc>" + new string('x', 400) + "</doc>";
        await File.WriteAllTextAsync(Path.Combine(suite.Root, "Alpha", "result", "other.xml"), wide, cancellationToken);

        var results = XIncludeRunner.Run(
            suite.Root,
            [.. XIncludeCatalogReader.Read(suite.Root).Where(c => c.Id == "alpha-differs")],
            cancellationToken
        );

        await Assert.That(results[0].Reason).Contains("...");
    }

    [Test]
    public async Task ExclusionReason_LiveHttpCase_IsExcluded()
    {
        var xcase = new XIncludeCase(
            "harold-87",
            "Harold",
            "Harold/a.xml",
            XIncludeCaseType.Success,
            [],
            null,
            string.Empty
        );

        await Assert.That(XIncludeRunner.ExclusionReason(xcase)).Contains("live http");
    }

    [Test]
    public async Task ExclusionReason_NoUnsupportedFeature_IsNull()
    {
        var xcase = new XIncludeCase("a", "A", "A/a.xml", XIncludeCaseType.Success, ["lang-fixup"], null, string.Empty);

        await Assert.That(XIncludeRunner.ExclusionReason(xcase)).IsNull();
    }
}
