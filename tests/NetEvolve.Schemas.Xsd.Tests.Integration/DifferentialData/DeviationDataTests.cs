namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

public class DeviationDataTests
{
    private static DeviationList Load() => DeviationList.Load(DeviationData.ListPath);

    [Test]
    public async Task List_HasEntries_ForTheDifferencesOfTheCorpus() =>
        await Assert.That(Load().Entries.Count).IsGreaterThan(0);

    [Test]
    public async Task List_EveryEntryHasACaseIdASpecificationReferenceAndAReason()
    {
        foreach (var entry in Load().Entries)
        {
            await Assert.That(string.IsNullOrWhiteSpace(entry.CaseId)).IsFalse();
            await Assert
                .That(string.IsNullOrWhiteSpace(entry.SpecificationReference))
                .IsFalse()
                .Because(entry.CaseId + " has no specification reference.");
            await Assert
                .That(string.IsNullOrWhiteSpace(entry.Reason))
                .IsFalse()
                .Because(entry.CaseId + " has no reason.");
        }
    }

    [Test]
    public async Task List_NeverNamesACaseThatTheKnownFailuresTrack()
    {
        var tracked = TrackedDefects.Load();

        await Assert.That(Load().Entries.Where(e => tracked.ContainsKey(e.CaseId))).IsEmpty();
    }

    [Test]
    public async Task Document_ListsTheCaseIdsOfTheDataFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = await File.ReadAllTextAsync(DeviationData.DocumentPath, cancellationToken);

        var inDocument = DeviationData.CaseIdsOf(document);
        var inData = Load()
            .Entries.Select(e => e.CaseId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

        await Assert.That(inDocument).IsEquivalentTo(inData);
    }

    [Test]
    public async Task Document_NamesTheBaselineRuntimeOfTheDataFile(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = await File.ReadAllTextAsync(DeviationData.DocumentPath, cancellationToken);

        await Assert.That(document).Contains("baseline-runtime: " + Load().BaselineRuntime);
    }

    [Test]
    public async Task CaseIdsOf_ReadsTheLinesInsideFencedBlocksOnly()
    {
        var ids = DeviationData.CaseIdsOf(
            "# Title\nnot-an-id\n```text\nset/1.0/g/a\n\n  set/1.0/g/b  \n```\nplain\n```\nset/1.0/g/a\n```\n"
        );

        await Assert.That(ids).IsEquivalentTo(["set/1.0/g/a", "set/1.0/g/b"]);
    }

    [Test]
    public async Task TrackedDefects_Read_KeepsOnlyTheCasesWithAnIssue()
    {
        var tracked = new Dictionary<string, string>(StringComparer.Ordinal);

        TrackedDefects.Read(
            "# comment\r\nsetA\t1.0/g/a\t#600\treason\r\nsetA\t1.0/g/b\t-\treason\r\n\r\nsetB\t1.0/g/c\t#12\treason\r\n",
            tracked
        );

        await Assert.That(tracked.Count).IsEqualTo(2);
        await Assert.That(tracked["setA/1.0/g/a"]).IsEqualTo("#600");
        await Assert.That(tracked["setB/1.0/g/c"]).IsEqualTo("#12");
    }

    [Test]
    public async Task TrackedDefects_Load_ReadsTheCommittedLists()
    {
        var tracked = TrackedDefects.Load();

        await Assert.That(tracked.Count).IsGreaterThan(0);
        await Assert.That(tracked.Values.All(v => v.Length > 1 && v[0] == '#')).IsTrue();
    }
}
