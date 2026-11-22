namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry;

using System.Threading;
using System.Threading.Tasks;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entry.DtdEntryFixture;

/// <summary>Every input kind of the validator ends in the same issues, with the same codes and locations.</summary>
public class DtdValidatorInputTests
{
    [Test]
    public async Task EverySynchronousInputKind_ReportsIdenticalIssuesWithCodeAndLocation()
    {
        var kinds = SyncKinds(Build(), TwoErrors);

        await Assert.That(kinds.Count).IsEqualTo(11);
        foreach (var (kind, result) in kinds)
        {
            await Assert.That(result.IsValid).IsFalse().Because(kind);
            await Assert.That(Describe(result)).IsEquivalentTo(TwoErrorsIssues).Because(kind);
        }
    }

    [Test]
    public async Task EveryAsynchronousInputKind_ReportsIdenticalIssuesWithCodeAndLocation(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var kinds = await AsyncKinds(Build(), TwoErrors, cancellationToken);

        await Assert.That(kinds.Count).IsEqualTo(5);
        foreach (var (kind, result) in kinds)
        {
            await Assert.That(Describe(result)).IsEquivalentTo(TwoErrorsIssues).Because(kind);
        }
    }

    [Test]
    public async Task EveryInputKind_AcceptsAValidDocument_WithoutASystemId(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validator = Build();
        var kinds = SyncKinds(validator, ValidNote, null);
        kinds.AddRange(await AsyncKinds(validator, ValidNote, cancellationToken, null));

        foreach (var (kind, result) in kinds)
        {
            await Assert.That(result.IsValid).IsTrue().Because(kind);
            await Assert.That(result.Issues).IsEmpty().Because(kind);
        }
    }

    [Test]
    public async Task EveryInputKind_WithoutASystemId_LocatesTheIssuesWithoutASource()
    {
        foreach (var (kind, result) in SyncKinds(Build(), TwoErrors, null))
        {
            await Assert.That(result.Issues.Count).IsEqualTo(3).Because(kind);
            await Assert.That(result.Issues[0].Location.Source).IsNull().Because(kind);
            await Assert.That(result.Issues[0].Location.Line).IsEqualTo(3).Because(kind);
        }
    }

    [Test]
    public async Task EveryInputKind_ReportsAnExternalSubsetOfTheDefaultBuilderWithXml2002()
    {
        foreach (var (kind, result) in SyncKinds(Build(), ExternalSubsetDocument))
        {
            await Assert.That(result.Issues.Count).IsEqualTo(1).Because(kind);
            await Assert.That(result.Issues[0].Code.ToString()).IsEqualTo("NES1202").Because(kind);
        }
    }

    [Test]
    public async Task Validate_ADocumentWithoutADoctype_ReportsTheMissingDeclaration()
    {
        var result = Run("<r/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert
            .That(Describe(result))
            .Contains("NES1610|Error|http://example.org/doc/main.xml|1|1|Missing Document Type Declaration");
    }

    [Test]
    public async Task Validate_ADocumentThatIsNotWellFormed_EndsWithTheErrorOfTheReader()
    {
        var result = Run("<!DOCTYPE r [<!ELEMENT r ANY>]><r></x>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.Issues[result.Issues.Count - 1].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues[result.Issues.Count - 1].Location.Source).IsEqualTo(SystemId);
    }
}
