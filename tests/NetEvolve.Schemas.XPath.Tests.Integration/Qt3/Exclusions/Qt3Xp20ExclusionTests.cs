namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Exclusions;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

public class Qt3Xp20ExclusionTests
{
    [Test]
    public async Task Files_EveryEntry_HasAReasonAndEveryDeferredFeatureItsDefId()
    {
        var entries = Qt3Xp20Exclusions.Load();

        await Assert.That(entries).IsNotEmpty();
        await Assert.That(Qt3Xp20Exclusions.Validate(entries)).IsEmpty();
    }

    [Test]
    public async Task Validate_EntryWithoutAReason_Fails()
    {
        var entry = new Qt3Xp20ExclusionEntry("set/case", "xml-version 1.1", " ", "");

        await Assert.That(Qt3Xp20Exclusions.Validate([entry])).HasSingleItem();
    }

    [Test]
    [Arguments("feature schemaImport", "DEF-40")]
    [Arguments("feature schemaValidation", "DEF-40")]
    [Arguments("feature staticTyping", "DEF-41")]
    [Arguments("feature namespace-axis", "DEF-42")]
    public async Task Validate_DeferredFeatureWithoutItsDefId_Fails(string dependency, string defId)
    {
        var without = new Qt3Xp20ExclusionEntry("set/case", dependency, "a reason", "");
        var with = new Qt3Xp20ExclusionEntry("set/case", dependency, "a reason", defId);

        await Assert.That(Qt3Xp20Exclusions.Validate([without])).HasSingleItem();
        await Assert.That(Qt3Xp20Exclusions.Validate([with])).IsEmpty();
    }

    [Test]
    public async Task Validate_UnknownDefId_Fails()
    {
        var entry = new Qt3Xp20ExclusionEntry("set/case", "xml-version 1.1", "a reason", "DEF-99");

        await Assert.That(Qt3Xp20Exclusions.Validate([entry])).HasSingleItem();
    }

    [Test]
    public async Task Files_CaseIds_AreUnique()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = Qt3Xp20Exclusions.Load().Select(static e => e.CaseId).Where(id => !seen.Add(id)).ToList();

        await Assert.That(duplicates).IsEmpty();
    }

    [Test]
    public async Task Files_ExcludedSet_EqualsTheExcludedSetOfTheSelection(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        var fromFiles = Qt3Xp20Exclusions
            .Load()
            .Select(static e => e.CaseId + "\t" + e.Dependencies)
            .Order(StringComparer.Ordinal);
        var fromSelection = selection
            .Excluded.Select(static e => e.CaseId + "\t" + e.Reason)
            .Order(StringComparer.Ordinal);

        await Assert.That(fromFiles.SequenceEqual(fromSelection)).IsTrue();
    }

    [Test]
    public async Task Files_NoCase_IsBothExcludedAndInABaseline()
    {
        var excluded = Qt3Xp20Exclusions.Load().Select(static e => e.CaseId);
        var baseline = Qt3Suite.LoadBaselines().Values.SelectMany(static b => b.Ids);

        var both = Overlap(excluded, baseline);

        await Assert.That(both).IsEmpty().Because(string.Join(Environment.NewLine, both));
    }

    [Test]
    public async Task Overlap_CaseInBothSets_IsReported()
    {
        var both = Overlap(["set/a", "set/b"], ["set/b", "set/c"]);

        await Assert.That(both).HasSingleItem();
        await Assert.That(both[0]).IsEqualTo("set/b");
    }

    private static List<string> Overlap(IEnumerable<string> excluded, IEnumerable<string> baseline) =>
        [.. excluded.Intersect(baseline, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>Rewrites the exclusion files; run it with the explicit filter after a deliberate change of the filter or of the pinned suite.</summary>
    [Test]
    [Explicit]
    public async Task Files_Regenerate(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        _ = Directory.CreateDirectory(Qt3Xp20Exclusions.DirectoryPath);
        foreach (var old in Directory.GetFiles(Qt3Xp20Exclusions.DirectoryPath, "*.txt"))
        {
            File.Delete(old);
        }
        foreach (var (name, text) in Qt3Xp20Exclusions.Format(selection.Excluded))
        {
            await File.WriteAllTextAsync(Path.Combine(Qt3Xp20Exclusions.DirectoryPath, name), text, cancellationToken);
        }

        await Assert.That(Qt3Xp20Exclusions.Load()).IsNotEmpty();
    }
}
