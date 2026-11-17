namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Text.RegularExpressions;

public partial class Qt3Xp20SelectionTests
{
    /// <summary>The number of XPath 2.0 cases the filter selects; a change of the filter or of the pinned suite makes it red.</summary>
    private const int SelectedXp20Cases = 15288;

    private const int TotalCases = 31821;

    [GeneratedRegex("^(spec|feature|xml-version|xsd-version) \\S")]
    private static partial Regex NamedDependency();

    [Test]
    public async Task Selection_Xp20_CountIsFixed()
    {
        await Assert.That(Qt3Suite.Selection.Selected.Count).IsEqualTo(SelectedXp20Cases);
        await Assert.That(Qt3Suite.Selection.Selected.Count + Qt3Suite.Selection.Excluded.Count).IsEqualTo(TotalCases);
    }

    [Test]
    public async Task Selection_XsDouble001_IsExcludedWithXsdVersionReason()
    {
        Qt3Exclusion? found = null;
        foreach (var exclusion in Qt3Suite.Selection.Excluded)
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
    public async Task Selection_EveryExclusion_CarriesReasonNamingTheDependency()
    {
        var unnamed = new List<string>();
        foreach (var exclusion in Qt3Suite.Selection.Excluded)
        {
            if (!NamedDependency().IsMatch(exclusion.Reason))
            {
                unnamed.Add(exclusion.CaseId);
            }
        }

        await Assert.That(unnamed).IsEmpty();
    }

    [Test]
    public async Task Selection_ExcludedFeatures_AreRecordedWithTheirReason()
    {
        var all = string.Join('\n', Qt3Suite.Selection.Excluded.Select(e => e.Reason));

        await Assert.That(all).Contains("feature schemaImport");
        await Assert.That(all).Contains("feature namespace-axis");
        await Assert.That(all).Contains("xml-version 1.1");
    }
}
