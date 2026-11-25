namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

public class Qt3BaselineTests
{
    /// <summary>Regenerates the baseline files from the current selection; run it with the explicit filter after a deliberate change.</summary>
    [Test]
    [Explicit]
    public async Task Baseline_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        Qt3Suite.RegenerateBaselines(selection, await Qt3Engine.GetSharedAsync(cancellationToken));

        await Assert.That(Qt3Suite.LoadBaselines().Count).IsEqualTo(selection.SelectedTestSets.Count);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_ListsOnlySelectedCases_EachOnceInItsOwnFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var owners = new Dictionary<string, int>(StringComparer.Ordinal);
        var foreign = new List<string>();
        foreach (var (testSet, baseline) in Qt3Suite.LoadBaselines())
        {
            foreach (var id in baseline.Ids)
            {
                owners[id] = owners.GetValueOrDefault(id) + 1;
                if (!id.StartsWith(testSet + "/", StringComparison.Ordinal))
                {
                    foreign.Add($"{testSet}.txt: {id}");
                }
            }
        }

        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in selection.Selected)
        {
            _ = selected.Add(testCase.Id);
        }

        var unselectedOrRepeated = new List<string>();
        foreach (var (id, count) in owners)
        {
            if (count != 1 || !selected.Contains(id))
            {
                unselectedOrRepeated.Add(id);
            }
        }

        await Assert.That(unselectedOrRepeated).IsEmpty();
        await Assert.That(foreign).IsEmpty();
        await Assert.That(Qt3Suite.LoadBaselines().Count).IsEqualTo(selection.SelectedTestSets.Count);
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_MatchesEngineRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var results = Qt3Runner.Run(selection.Selected, await Qt3Engine.GetSharedAsync(cancellationToken));
        var baselines = Qt3Suite.LoadBaselines();
        var listed = 0;
        foreach (var baseline in baselines.Values)
        {
            listed += baseline.Count;
        }

        var report = ConformanceReport.Compare(results, baselines);

        await Assert.That(report.IsSuccess).IsTrue();
        await Assert.That(report.Counts.Passed).IsGreaterThan(0);
        await Assert.That(report.Counts.Failed + report.Counts.Skipped).IsEqualTo(listed);
        await Assert
            .That(report.Counts.Passed + report.Counts.Failed + report.Counts.Skipped)
            .IsEqualTo(selection.Selected.Count);
    }

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
                || !fields[1].StartsWith('#')
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
