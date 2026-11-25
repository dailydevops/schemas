namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class Qt3Xp20SelectionTests
{
    /// <summary>The number of XPath 2.0 cases the filter selects; a change of the filter or of the pinned suite makes it red.</summary>
    private const int SelectedXp20Cases = 15288;

    internal const int TotalCases = 31821;

    private static readonly string[] DependencyTypes = ["spec ", "feature ", "xml-version ", "xsd-version "];

    [Test]
    public async Task Selection_Xp20_CountIsFixed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        await Assert.That(selection.Selected.Count).IsEqualTo(SelectedXp20Cases);
        await Assert.That(selection.Selected.Count + selection.Excluded.Count).IsEqualTo(TotalCases);
    }

    [Test]
    public async Task Selection_XsDouble001_IsExcludedWithXsdVersionReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        Qt3Exclusion? found = null;
        foreach (var exclusion in selection.Excluded)
        {
            if (exclusion.CaseId.EndsWith("/xs-double-001", StringComparison.Ordinal))
            {
                found = exclusion;
            }
        }

        await Assert.That(found).IsNotNull();
        await Assert.That(found!.Reason).Contains("xsd-version");
    }

    [Test]
    public async Task Selection_EveryExclusion_CarriesReasonNamingTheDependency(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var unnamed = new List<string>();
        foreach (var exclusion in selection.Excluded)
        {
            var named = false;
            foreach (var type in DependencyTypes)
            {
                named |= exclusion.Reason.StartsWith(type, StringComparison.Ordinal);
            }
            if (!named)
            {
                unnamed.Add(exclusion.CaseId);
            }
        }

        await Assert.That(unnamed).IsEmpty();
    }

    [Test]
    public async Task Selection_ExcludedFeatures_AreRecordedWithTheirReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);
        var all = string.Join(';', selection.Excluded.Select(e => e.Reason));

        await Assert.That(all).Contains("feature schemaImport");
        await Assert.That(all).Contains("feature namespace-axis");
        await Assert.That(all).Contains("xml-version 1.1");
    }
}
