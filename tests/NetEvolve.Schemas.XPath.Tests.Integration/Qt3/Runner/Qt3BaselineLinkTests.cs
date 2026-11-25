namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Globalization;

/// <summary>Every baseline entry links a known open issue; these tests need no network and run in the default set.</summary>
public class Qt3BaselineLinkTests
{
    /// <summary>The links file: one line per baseline entry, case id, <c>#issue</c> and reason separated by tabs.</summary>
    private static readonly string LinksPath = Path.Combine(Qt3Suite.BaselineDirectory, "known-failure-links.tsv");

    /// <summary>The committed list of known open issue numbers, one per line; the guard never asks GitHub.</summary>
    private static readonly string OpenIssuesPath = Path.Combine(Qt3Suite.BaselineDirectory, "known-open-issues.lst");

    [Test]
    public async Task Baseline_EveryEntry_LinksAKnownOpenIssue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = Qt3Suite.LoadBaselines().Values.SelectMany(baseline => baseline.Ids).ToList();
        var links = ParseLinks(await File.ReadAllTextAsync(LinksPath, cancellationToken));
        var open = ParseIssues(await File.ReadAllTextAsync(OpenIssuesPath, cancellationToken));

        var problems = Verify(ids, links, open);

        await Assert.That(ids).IsNotEmpty();
        await Assert.That(problems).IsEmpty().Because(string.Join('\n', problems));
    }

    [Test]
    public async Task Verify_EntryWithoutLink_IsAProblem()
    {
        var problems = Verify(["set/a", "set/b"], ParseLinks("set/a\t#7\treason\n"), [7]);

        await Assert.That(problems).HasSingleItem();
        await Assert.That(problems[0]).Contains("set/b");
    }

    [Test]
    public async Task Verify_LinkToIssueOutsideTheOpenList_IsAProblem()
    {
        var problems = Verify(["set/a"], ParseLinks("set/a\t#8\treason\n"), [7]);

        await Assert.That(problems).HasSingleItem();
        await Assert.That(problems[0]).Contains("#8");
    }

    [Test]
    public async Task Verify_LinkWithoutBaselineEntry_IsAProblem()
    {
        var problems = Verify([], ParseLinks("set/a\t#7\treason\n"), [7]);

        await Assert.That(problems).HasSingleItem();
        await Assert.That(problems[0]).Contains("set/a");
    }

    [Test]
    public async Task ParseLinks_LineWithoutIssueNumber_Throws() =>
        await Assert.That(() => ParseLinks("set/a\tseven\treason\n")).Throws<InvalidDataException>();

    private static List<(string CaseId, int Issue)> ParseLinks(string text)
    {
        var links = new List<(string, int)>();
        foreach (var line in DataLines(text))
        {
            var fields = line.Split('\t');
            if (
                fields.Length != 3
                || fields[0].Length == 0
                || string.IsNullOrWhiteSpace(fields[2])
                || fields[1].Length < 2
                || fields[1][0] != '#'
                || !int.TryParse(fields[1].AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var issue)
            )
            {
                throw new InvalidDataException($"Malformed link line '{line}': expected case id, #issue, reason.");
            }

            links.Add((fields[0], issue));
        }

        return links;
    }

    private static HashSet<int> ParseIssues(string text) =>
        [.. DataLines(text).Select(line => int.Parse(line, NumberStyles.None, CultureInfo.InvariantCulture))];

    private static IEnumerable<string> DataLines(string text) =>
        text.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0 && line[0] != '#');

    private static List<string> Verify(
        IReadOnlyCollection<string> baselineIds,
        List<(string CaseId, int Issue)> links,
        HashSet<int> openIssues
    )
    {
        var linked = new HashSet<string>(links.Select(link => link.CaseId), StringComparer.Ordinal);
        var baseline = new HashSet<string>(baselineIds, StringComparer.Ordinal);
        var problems = baselineIds
            .Where(id => !linked.Contains(id))
            .Select(id => $"The baseline entry '{id}' links no bug issue.")
            .ToList();
        foreach (var (caseId, issue) in links)
        {
            if (!baseline.Contains(caseId))
            {
                problems.Add($"The link of '{caseId}' to #{issue} has no baseline entry; remove it.");
            }

            if (!openIssues.Contains(issue))
            {
                problems.Add($"'{caseId}' links #{issue}, which is not in known-open-issues.lst.");
            }
        }

        return problems;
    }
}
