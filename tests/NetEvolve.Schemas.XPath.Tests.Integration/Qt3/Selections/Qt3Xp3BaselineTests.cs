namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class Qt3Xp3BaselineTests
{
    /// <summary>Rewrites the recorded per test set counts; run it with the explicit filter after a deliberate change of the rules or of the pinned suite.</summary>
    [Test]
    [Explicit]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Counts_Regenerate(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(level, cancellationToken);

        await File.WriteAllTextAsync(
            Qt3Xp3Selections.CountsFileOf(level),
            Qt3Xp3Selections.FormatCounts(selection),
            cancellationToken
        );

        await Assert.That(File.Exists(Qt3Xp3Selections.CountsFileOf(level))).IsTrue();
    }

    [Test]
    public async Task Tiers_EveryCaseOfTheSelections_IsInExactlyOneBaselineTier(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xp20 = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var xp30 = await Qt3Xp3Selections.GetSelectionAsync(Qt3SpecLevel.XPath30, cancellationToken);
        var xp31 = await Qt3Xp3Selections.GetSelectionAsync(Qt3SpecLevel.XPath31, cancellationToken);
        var new30 = await Qt3Xp3Selections.GetNewCasesAsync(Qt3SpecLevel.XPath30, cancellationToken);
        var new31 = await Qt3Xp3Selections.GetNewCasesAsync(Qt3SpecLevel.XPath31, cancellationToken);
        var tiers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var selection in new[] { xp20, new30, new31 })
        {
            foreach (var testCase in selection.Selected)
            {
                tiers[testCase.Id] = tiers.GetValueOrDefault(testCase.Id) + 1;
            }
        }

        var unaccounted = new List<string>();
        foreach (var testCase in xp30.Selected.Concat(xp31.Selected))
        {
            if (tiers.GetValueOrDefault(testCase.Id) != 1)
            {
                unaccounted.Add(testCase.Id);
            }
        }
        var repeated = tiers.Where(t => t.Value != 1).Select(t => t.Key).ToList();

        await Assert.That(unaccounted).IsEmpty();
        await Assert.That(repeated).IsEmpty();
        await Assert.That(new30.Selected.Count).IsGreaterThan(0);
        await Assert.That(new31.Selected.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Tiers_NewCasesOfTheXPath30Selection_AreNotInTheXPath20Selection(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xp20 = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var xp30 = await Qt3Xp3Selections.GetSelectionAsync(Qt3SpecLevel.XPath30, cancellationToken);
        var new30 = await Qt3Xp3Selections.GetNewCasesAsync(Qt3SpecLevel.XPath30, cancellationToken);

        await Assert.That(new30.Selected.Count + CountShared(xp20, xp30)).IsEqualTo(xp30.Selected.Count);
    }

    [Test]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Baseline_ListsOnlyNewCases_EachOnceInItsOwnFile(
        Qt3SpecLevel level,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newCases = await Qt3Xp3Selections.GetNewCasesAsync(level, cancellationToken);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in newCases.Selected)
        {
            _ = ids.Add(testCase.Id);
        }
        var stray = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (testSet, baseline) in Qt3Xp3Selections.LoadBaselines(level))
        {
            foreach (var id in baseline.Ids)
            {
                if (!ids.Contains(id) || !id.StartsWith(testSet + "/", StringComparison.Ordinal) || !seen.Add(id))
                {
                    stray.Add($"{testSet}.txt: {id}");
                }
            }
        }

        await Assert.That(stray).IsEmpty();
    }

    [Test]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Baseline_EveryTestSetWithNewCases_HasItsOwnFile(
        Qt3SpecLevel level,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newCases = await Qt3Xp3Selections.GetNewCasesAsync(level, cancellationToken);

        var baselines = Qt3Xp3Selections.LoadBaselines(level);

        await Assert.That(baselines.Keys).IsEquivalentTo(newCases.SelectedTestSets);
    }

    [Test]
    public async Task Baseline_TheDirectoriesOfTheTiers_AreSeparateFromTheXPath20Baselines()
    {
        var directories = new[]
        {
            Qt3Suite.BaselineDirectory,
            Qt3Xp3Selections.BaselineDirectoryOf(Qt3SpecLevel.XPath30),
            Qt3Xp3Selections.BaselineDirectoryOf(Qt3SpecLevel.XPath31),
        };

        await Assert.That(directories.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(3);
        await Assert.That(Path.GetFileName(directories[1])).IsEqualTo("qt3-xp30");
        await Assert.That(Path.GetFileName(directories[2])).IsEqualTo("qt3-xp31");
    }

    private static int CountShared(Qt3Selection first, Qt3Selection second)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in first.Selected)
        {
            _ = ids.Add(testCase.Id);
        }
        var shared = 0;
        foreach (var testCase in second.Selected)
        {
            if (ids.Contains(testCase.Id))
            {
                shared++;
            }
        }
        return shared;
    }
}
