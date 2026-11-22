namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// The XPath 3.0 and 3.1 selections of the pinned QT3 suite and their baselines. A baseline holds the cases of its tier only: the
/// cases the selection adds to the XPath 2.0 selection (<c>tests/conformance/baselines/qt3</c>) and, for XPath 3.1, to the XPath 3.0
/// selection. A case that several selections share is recorded once, in the first tier that selects it.
/// </summary>
internal static class Qt3Xp3Selections
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly Dictionary<Qt3SpecLevel, Qt3Selection> Selections = [];
    private static readonly Dictionary<Qt3SpecLevel, Qt3Selection> NewCases = [];
    private static IReadOnlyList<Qt3TestSet>? s_testSets;

    public static string BaselineDirectoryOf(Qt3SpecLevel level) =>
        Path.Combine(Qt3Suite.RepositoryRoot, "tests", "conformance", "baselines", Name(level));

    public static string CountsFileOf(Qt3SpecLevel level) =>
        Path.Combine(
            Qt3Suite.RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.XPath.Tests.Integration",
            "Qt3",
            "Selections",
            "selected-" + Name(level)[4..] + ".txt"
        );

    /// <summary>Formats the number of selected cases per test set: <c>&lt;test-set&gt;&lt;tab&gt;&lt;count&gt;</c>, sorted, one line each.</summary>
    /// <param name="selection">The selection.</param>
    /// <returns>The text with <c>\n</c> line endings.</returns>
    public static string FormatCounts(Qt3Selection selection)
    {
        var names = new List<string>(selection.SelectedTestSets);
        names.Sort(StringComparer.Ordinal);
        var text = new StringBuilder();
        foreach (var name in names)
        {
            _ = text.Append(name).Append('\t').Append(selection.SelectedIn(name).Count).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>Gets the whole selection of one level.</summary>
    /// <param name="level">The level.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The selection.</returns>
    public static async Task<Qt3Selection> GetSelectionAsync(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await SelectionCoreAsync(level, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Gets the cases of one level that no lower tier records: not selected by XPath 2.0 and, for XPath 3.1, not by XPath 3.0.</summary>
    /// <param name="level">The level.</param>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The cases in catalog order.</returns>
    public static async Task<Qt3Selection> GetNewCasesAsync(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var xp20 = await Qt3Suite.GetSelectionAsync(cancellationToken).ConfigureAwait(false);
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (NewCases.TryGetValue(level, out var newCases))
            {
                return newCases;
            }

            var lower = new HashSet<string>(StringComparer.Ordinal);
            AddIds(lower, xp20);
            if (level == Qt3SpecLevel.XPath31)
            {
                AddIds(lower, await SelectionCoreAsync(Qt3SpecLevel.XPath30, cancellationToken).ConfigureAwait(false));
            }

            var selection = await SelectionCoreAsync(level, cancellationToken).ConfigureAwait(false);
            var cases = new List<Qt3Case>();
            foreach (var testCase in selection.Selected)
            {
                if (!lower.Contains(testCase.Id))
                {
                    cases.Add(testCase);
                }
            }
            NewCases.Add(level, newCases = new Qt3Selection(cases, []));
            return newCases;
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    /// <summary>Loads the baseline of every test set that has a file in the directory of the tier, keyed by test set name.</summary>
    /// <param name="level">The level.</param>
    /// <returns>The baselines.</returns>
    public static Dictionary<string, KnownFailureBaseline> LoadBaselines(Qt3SpecLevel level)
    {
        var directory = BaselineDirectoryOf(level);
        var baselines = new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal);
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, "*.txt"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                baselines.Add(name, BaselineFiles.Load(directory, name));
            }
        }
        return baselines;
    }

    /// <summary>
    /// Rewrites the baseline files of one tier: one file per test set with new cases, none for any other set, each listing the cases
    /// that do not pass with the engine.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="newCases">The cases of the tier.</param>
    /// <param name="engine">The engine that decides which cases pass.</param>
    public static void RegenerateBaselines(Qt3SpecLevel level, Qt3Selection newCases, IQt3Engine engine)
    {
        var directory = BaselineDirectoryOf(level);
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory, "*.txt"))
            {
                File.Delete(file);
            }
        }
        foreach (var testSet in newCases.SelectedTestSets)
        {
            var ids = new List<string>();
            foreach (var testCase in newCases.SelectedIn(testSet))
            {
                if (Qt3Runner.Run(testCase, engine).Outcome != CaseOutcome.Pass)
                {
                    ids.Add(testCase.Id);
                }
            }
            BaselineFiles.Save(directory, testSet, ids);
        }
    }

    private static string Name(Qt3SpecLevel level) => level == Qt3SpecLevel.XPath30 ? "qt3-xp30" : "qt3-xp31";

    private static void AddIds(HashSet<string> ids, Qt3Selection selection)
    {
        foreach (var testCase in selection.Selected)
        {
            _ = ids.Add(testCase.Id);
        }
    }

    // Callers hold the gate.
    private static async Task<Qt3Selection> SelectionCoreAsync(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Selections.TryGetValue(level, out var selection))
        {
            s_testSets ??= Qt3CatalogReader.Read(await Qt3Suite.GetRootAsync(cancellationToken).ConfigureAwait(false));
            Selections.Add(level, selection = Qt3Xp3Selector.Select(s_testSets, level));
        }
        return selection;
    }
}
