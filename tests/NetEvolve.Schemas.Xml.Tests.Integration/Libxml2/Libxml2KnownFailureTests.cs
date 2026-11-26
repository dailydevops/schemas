namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Baseline;

/// <summary>The baselines of the libxml2 sub-trees and their reasons; both are committed, so the guard needs no download.</summary>
public class Libxml2KnownFailureTests
{
    private static List<(string CaseId, string Kind, string Reason)> ReadLinks(string name)
    {
        var path = Path.Combine(Libxml2Suite.BaselineDirectory(name), "known-failure-links.tsv");
        if (!File.Exists(path))
        {
            return [];
        }

        var links = new List<(string, string, string)>();
        foreach (var line in File.ReadAllLines(path).Where(l => l.Length > 0 && l[0] != '#'))
        {
            var fields = line.Split('\t');
            if (fields.Length != 3 || !IsIssueOrLeniency(fields[1]))
            {
                throw new InvalidDataException($"The line '{line}' is not 'case id, #issue or leniency, reason'.");
            }

            links.Add((fields[0], fields[1], fields[2]));
        }

        return links;
    }

    private static bool IsIssueOrLeniency(string kind) =>
        kind == "leniency"
        || (
            kind.Length > 1
            && kind[0] == '#'
            && int.TryParse(kind.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            && n > 0
        );

    private static IEnumerable<string> BaselineIds(string name) =>
        Directory
            .GetFiles(Libxml2Suite.BaselineDirectory(name), "*.txt")
            .SelectMany(f =>
                BaselineFiles.Load(Libxml2Suite.BaselineDirectory(name), Path.GetFileNameWithoutExtension(f)).Ids
            );

    [Test]
    [Arguments("libxml2-errors")]
    [Arguments("libxml2-regexp")]
    public async Task Links_EveryLine_HasAKindAndAReason_AndNoCaseIsListedTwice(
        string name,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var links = ReadLinks(name);

        await Assert.That(links.All(l => !string.IsNullOrWhiteSpace(l.Reason))).IsTrue();
        await Assert.That(links.Select(l => l.CaseId).Distinct(StringComparer.Ordinal).Count()).IsEqualTo(links.Count);
    }

    [Test]
    [Arguments("libxml2-errors")]
    [Arguments("libxml2-regexp")]
    public async Task Links_AndBaselineFiles_ListTheSameCases(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = Directory.Exists(Libxml2Suite.BaselineDirectory(name)) ? BaselineIds(name).ToList() : [];

        await Assert.That(ids).IsEquivalentTo(ReadLinks(name).Select(l => l.CaseId));
    }
}
