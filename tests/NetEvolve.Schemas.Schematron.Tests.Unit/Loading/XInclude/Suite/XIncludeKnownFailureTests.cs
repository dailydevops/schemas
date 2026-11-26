namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Baseline;

/// <summary>The baseline of the W3C XInclude test suite and its links to issues; both are committed, so the guard needs no suite download.</summary>
public class XIncludeKnownFailureTests
{
    private static readonly string[] TestSets = ["Imaq", "FourThought", "Nist", "EdUni", "Harold"];

    private static List<(string CaseId, int Issue, string Reason)> ReadLinks()
    {
        var links = new List<(string, int, string)>();
        var path = Path.Combine(XIncludeSuite.BaselineDirectory, "known-failure-links.tsv");
        foreach (var line in File.ReadAllLines(path).Where(l => l.Length > 0 && l[0] != '#'))
        {
            var fields = line.Split('\t');
            if (
                fields.Length != 3
                || fields[1].Length < 2
                || fields[1][0] != '#'
                || !int.TryParse(fields[1].AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var issue)
            )
            {
                throw new InvalidDataException($"The line '{line}' is not 'case id, #issue, reason'.");
            }

            links.Add((fields[0], issue, fields[2]));
        }

        return links;
    }

    [Test]
    public async Task Links_EveryLine_HasAnIssueAndAReason_AndNoCaseIsListedTwice()
    {
        var links = ReadLinks();

        await Assert.That(links).IsNotEmpty();
        await Assert.That(links.All(l => l.Issue > 0 && !string.IsNullOrWhiteSpace(l.Reason))).IsTrue();
        await Assert.That(links.Select(l => l.CaseId).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(links.Count);
    }

    [Test]
    public async Task Links_AndBaselineFiles_ListTheSameCases()
    {
        var baselineIds = TestSets.SelectMany(s => BaselineFiles.Load(XIncludeSuite.BaselineDirectory, s).Ids).ToList();
        var linked = ReadLinks().Select(l => l.CaseId).ToList();

        await Assert.That(baselineIds).IsNotEmpty();
        await Assert.That(baselineIds).IsEquivalentTo(linked);
    }

    [Test]
    public async Task Baseline_HasNoFilesOtherThanTheContributorFolders()
    {
        var files = Directory
            .GetFiles(XIncludeSuite.BaselineDirectory, "*.txt")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        await Assert.That(files.All(f => TestSets.Contains(f))).IsTrue();
    }
}
