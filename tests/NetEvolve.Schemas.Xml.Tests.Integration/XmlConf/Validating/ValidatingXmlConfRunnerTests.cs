namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

using System.Text;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>The judgment of the validating runner on small documents; no suite is needed.</summary>
public class ValidatingXmlConfRunnerTests
{
    private const string Uri = "file:///doc.xml";

    private static DtdValidationOptions Options => DtdValidationOptions.Default;

    private static XmlConfCase Case(string id, XmlConfType type, bool namespaces = true) =>
        new(id, "set", id + ".xml", type, XmlConfEntities.None, [], [], "XML1.0", namespaces, string.Empty);

    private static string? Judge(XmlConfType type, string xml) =>
        ValidatingXmlConfRunner.Judge(type, ValidatingXmlConfRunner.Observe(Encoding.UTF8.GetBytes(xml), Uri, Options));

    private const string Valid = "<!DOCTYPE r [<!ELEMENT r EMPTY>]><r/>";
    private const string Invalid = "<!DOCTYPE r [<!ELEMENT r EMPTY>]><r>x</r>";
    private const string OnlyAWarning =
        "<!DOCTYPE r [<!ELEMENT r ((a,b)|(a,c))><!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>]><r><a/><b/></r>";

    [Test]
    public async Task Judge_ValidCase_WithoutIssue_Passes() =>
        await Assert.That(Judge(XmlConfType.Valid, Valid)).IsNull();

    [Test]
    public async Task Judge_ValidCase_WithAnError_FailsAndNamesTheCode()
    {
        var reason = Judge(XmlConfType.Valid, Invalid);

        await Assert.That(reason).Contains("XML5004");
    }

    [Test]
    public async Task Judge_ValidCase_WithOnlyAWarning_Passes() =>
        await Assert.That(Judge(XmlConfType.Valid, OnlyAWarning)).IsNull();

    [Test]
    public async Task Judge_InvalidCase_WithAnError_Passes() =>
        await Assert.That(Judge(XmlConfType.Invalid, Invalid)).IsNull();

    [Test]
    public async Task Judge_InvalidCase_WithoutIssue_Fails() =>
        await Assert.That(Judge(XmlConfType.Invalid, Valid)).Contains("no validity issue");

    [Test]
    public async Task Judge_InvalidCase_WithOnlyAWarning_Fails() =>
        await Assert.That(Judge(XmlConfType.Invalid, OnlyAWarning)).Contains("no validity issue");

    [Test]
    [Arguments(XmlConfType.Valid)]
    [Arguments(XmlConfType.Invalid)]
    public async Task Judge_FatalStatus_FailsEitherType(XmlConfType type) =>
        await Assert.That(Judge(type, "<r>")).Contains("XML0001");

    [Test]
    public async Task Judge_ValidCase_WithoutDoctype_FailsWithTheMissingDeclaration() =>
        await Assert.That(Judge(XmlConfType.Valid, "<r/>")).Contains("XML5910");

    private static CaseResult Of(IReadOnlyList<CaseResult> results, string id) =>
        results.Single(r => string.Equals(r.CaseId, id, StringComparison.Ordinal));

    [Test]
    public async Task RunAsync_ExcludedCase_IsSkippedWithItsReason_AndOthersRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new FixtureDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "ok.xml"), Valid, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "bad.xml"), Invalid, cancellationToken);
        var selection = new XmlConfSelection(
            [
                Case("ok", XmlConfType.Valid),
                Case("bad", XmlConfType.Valid),
                Case("gone", XmlConfType.Valid),
                Case("skip", XmlConfType.Valid, namespaces: false),
                Case("wf", XmlConfType.NotWellFormed),
            ],
            []
        );

        var results = await ValidatingXmlConfRunner.RunAsync(
            selection,
            directory.Path,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["skip"] = "not run" },
            cancellationToken
        );

        await Assert.That(results.Select(r => r.CaseId)).IsEquivalentTo(["ok", "bad", "gone", "skip"]);
        await Assert.That(Of(results, "ok").Outcome).IsEqualTo(CaseOutcome.Pass);
        await Assert.That(Of(results, "bad").Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(Of(results, "gone").Reason).Contains("missing");
        await Assert.That(Of(results, "skip").Outcome).IsEqualTo(CaseOutcome.Skipped);
        await Assert.That(Of(results, "skip").Reason).IsEqualTo("not run");
    }

    [Test]
    public async Task RunAsync_NullArguments_Throw(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = new XmlConfSelection([], []);
        var none = new Dictionary<string, string>(StringComparer.Ordinal);

        await Assert
            .That(async () => await ValidatingXmlConfRunner.RunAsync(null!, "x", none, cancellationToken))
            .Throws<ArgumentNullException>();
        await Assert
            .That(async () => await ValidatingXmlConfRunner.RunAsync(selection, null!, none, cancellationToken))
            .Throws<ArgumentNullException>();
        await Assert
            .That(async () => await ValidatingXmlConfRunner.RunAsync(selection, "x", null!, cancellationToken))
            .Throws<ArgumentNullException>();
    }
}
