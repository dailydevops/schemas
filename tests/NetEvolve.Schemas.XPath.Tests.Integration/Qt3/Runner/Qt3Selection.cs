namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>The result of the dependency filter.</summary>
internal sealed class Qt3Selection
{
    private readonly Dictionary<string, List<Qt3Case>> _bySet = new(StringComparer.Ordinal);

    public Qt3Selection(IReadOnlyList<Qt3Case> selected, IReadOnlyList<Qt3Exclusion> excluded)
    {
        Selected = selected;
        Excluded = excluded;
        var names = new List<string>();
        foreach (var testCase in selected)
        {
            if (!_bySet.TryGetValue(testCase.TestSet, out var list))
            {
                list = [];
                _bySet.Add(testCase.TestSet, list);
                names.Add(testCase.TestSet);
            }
            list.Add(testCase);
        }
        SelectedTestSets = names;
    }

    /// <summary>Gets the selected cases in catalog order.</summary>
    public IReadOnlyList<Qt3Case> Selected { get; }

    /// <summary>Gets the excluded cases with their reasons.</summary>
    public IReadOnlyList<Qt3Exclusion> Excluded { get; }

    /// <summary>Gets the names of the test sets that have at least one selected case.</summary>
    public IReadOnlyList<string> SelectedTestSets { get; }

    /// <summary>Gets the selected cases of one test set.</summary>
    /// <param name="testSet">The test set name.</param>
    /// <returns>The cases, empty when the set has none.</returns>
    public IReadOnlyList<Qt3Case> SelectedIn(string testSet) => _bySet.TryGetValue(testSet, out var list) ? list : [];
}
