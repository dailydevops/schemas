namespace NetEvolve.Schemas.Tests.Unit.Resources.Libxml2;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>The runner of the libxml2 URI file, checked with inline data so no download is needed.</summary>
public class Libxml2UriRunnerTests
{
    [Test]
    public async Task Run_MatchingResults_PassEveryCase()
    {
        var results = Libxml2UriRunner.Run(
            "\nbar#baz\r\n../up\n",
            "http://foo.com/path/to/index.html?orig\nhttp://foo.com/path/to/bar#baz\nhttp://foo.com/path/up\n"
        );

        await Assert.That(results.Select(r => r.CaseId)).IsEquivalentTo(["uri.data/1", "uri.data/2", "uri.data/3"]);
        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
    }

    [Test]
    public async Task Run_DifferentResult_FailsOnlyThatCase()
    {
        var results = Libxml2UriRunner.Run("bar\nbaz\n", "http://foo.com/path/to/bar\nhttp://foo.com/other\n");

        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(results[1].Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(results[1].Reason).Contains("baz");
    }

    [Test]
    public async Task Run_ErrorLine_PassesWhenTheReferenceIsRefused()
    {
        var results = Libxml2UriRunner.Run("a#b#c\n", "::ERROR::\n");
        var accepted = Libxml2UriRunner.Run("bar\n", "::ERROR::\n");

        await Assert.That(results[0].Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(accepted[0].Outcome).IsEqualTo(CaseOutcome.Fail);
    }

    [Test]
    public async Task Run_DifferentLineCounts_Throws() =>
        await Assert
            .That(() => Libxml2UriRunner.Run("a\nb\n", "http://foo.com/path/to/a\n"))
            .Throws<InvalidDataException>();
}
