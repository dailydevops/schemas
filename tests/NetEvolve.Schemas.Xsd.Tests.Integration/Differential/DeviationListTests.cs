namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

public class DeviationListTests
{
    private const string Entry =
        "set/group/c1 | InstanceVerdict | instance verdict: ours=Valid, baseline=Invalid | XSD 1.0 Part 1, 3.4.4 | The baseline rejects a legal particle.";

    [Test]
    public async Task Parse_HeaderCommentsAndEntries_ReadsTheFiveFields()
    {
        var list = DeviationList.Parse($"baseline-runtime: 10.0\r\n# a comment\r\n\r\n{Entry}\r\n");

        await Assert.That(list.BaselineRuntime).IsEqualTo("10.0");
        await Assert
            .That(list.Entries)
            .IsEquivalentTo([
                new DeviationEntry(
                    "set/group/c1",
                    DifferenceKind.InstanceVerdict,
                    "instance verdict: ours=Valid, baseline=Invalid",
                    "XSD 1.0 Part 1, 3.4.4",
                    "The baseline rejects a legal particle."
                ),
            ]);
    }

    [Test]
    public async Task Find_ByCaseAndKind_ReturnsTheEntry()
    {
        var list = DeviationList.Parse($"baseline-runtime: 10.0\n{Entry}\n");

        await Assert.That(list.Find("set/group/c1", DifferenceKind.InstanceVerdict)).IsNotNull();
        await Assert.That(list.Find("set/group/c1", DifferenceKind.SchemaVerdict)).IsNull();
        await Assert.That(list.Find("other", DifferenceKind.InstanceVerdict)).IsNull();
    }

    [Test]
    [Arguments("")]
    [Arguments("# only a comment\n")]
    [Arguments("baseline-runtime: \n")]
    [Arguments("baseline-runtime: ten\n")]
    [Arguments("baseline-runtime: 10.0.1\n")]
    public async Task Parse_MissingOrMalformedHeader_Throws(string text) =>
        await Assert.That(() => DeviationList.Parse(text)).Throws<DeviationFormatException>();

    [Test]
    public async Task Parse_EntryBeforeTheHeader_Throws() =>
        await Assert
            .That(() => DeviationList.Parse($"{Entry}\nbaseline-runtime: 10.0\n"))
            .Throws<DeviationFormatException>();

    [Test]
    [Arguments("c1 | InstanceVerdict | observed | reference")]
    [Arguments("c1 | InstanceVerdict | observed | reference | reason | extra")]
    [Arguments("c1 | Unknown | observed | reference | reason")]
    [Arguments("c1 | instanceverdict | observed | reference | reason")]
    [Arguments("c1 | InstanceVerdict |  | reference | reason")]
    [Arguments(" | InstanceVerdict | observed | reference | reason")]
    [Arguments("c1 | InstanceVerdict | observed | reference | ")]
    public async Task Parse_MalformedEntry_Throws(string line) =>
        await Assert
            .That(() => DeviationList.Parse($"baseline-runtime: 10.0\n{line}\n"))
            .Throws<DeviationFormatException>();

    [Test]
    public async Task Parse_TwoEntriesForTheSameCaseAndKind_Throws() =>
        await Assert
            .That(() => DeviationList.Parse($"baseline-runtime: 10.0\n{Entry}\n{Entry}\n"))
            .Throws<DeviationFormatException>();

    [Test]
    public async Task Load_TheCommittedList_ParsesAndNamesTheBaselineRuntime()
    {
        var list = DeviationList.Load(RepositoryFiles.DeviationListPath);

        await Assert.That(list.BaselineRuntime).IsEqualTo("10.0");
    }
}
