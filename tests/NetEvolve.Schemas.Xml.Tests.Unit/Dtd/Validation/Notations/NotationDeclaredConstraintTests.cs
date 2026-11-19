namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationModel;

/// <summary>
/// Validity constraints Notation Declared (XML 1.0, section 4.2.2) and Unique Notation Name (section 4.7), and the notation
/// names that a NOTATION attribute type lists.
/// </summary>
public class NotationDeclaredConstraintTests
{
    // Notation Declared: the notation named by an unparsed entity declaration

    [Test]
    public async Task NotationDeclared_UnparsedEntityNamingAnUndeclaredNotation_ReportsXml5025_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel().Entity(Unparsed("logo", "gif")).Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("Notation Declared");
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5025.Message);
        await Assert.That(issues[0].Message).Contains("'logo'");
        await Assert.That(issues[0].Message).Contains("'gif'");
    }

    [Test]
    public async Task NotationDeclared_UnparsedEntityNamingADeclaredNotation_ReportsNothing()
    {
        var model = new NotationModel().Entity(Unparsed("logo", "gif")).Notation("gif").Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NotationDeclared_NotationDeclaredAfterTheEntity_IsAccepted()
    {
        const string subset = "<!ENTITY logo SYSTEM \"logo.gif\" NDATA gif><!NOTATION gif SYSTEM \"gif.exe\">";
        var info = new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        await Assert.That(error).IsNull();
        await Assert.That(Begin(declarations!).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NotationDeclared_ParsedAndParameterEntities_NameNoNotation()
    {
        var model = new NotationModel().Entity(Parsed("copy")).Entity(Parameter("pe")).Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NotationDeclared_NotationNamesAreCaseSensitive()
    {
        var model = new NotationModel().Entity(Unparsed("logo", "GIF")).Notation("gif").Build();

        await Assert.That(Begin(model).Count).IsEqualTo(1);
    }

    [Test]
    public async Task NotationDeclared_EveryOffendingEntityIsReportedOnce()
    {
        var model = new NotationModel()
            .Entity(Unparsed("a", "x"))
            .Entity(Unparsed("b", "gif"))
            .Entity(Unparsed("c", "x"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'a'");
        await Assert.That(issues[1].Message).Contains("'c'");
    }

    // The notation names that a NOTATION attribute type lists must be declared: reported as Notation Declared

    [Test]
    public async Task ListedNotation_UndeclaredInANotationAttributeType_ReportsXml5025_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("fmt", "gif", "png"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
        await Assert.That(issues[0].Constraint).IsEqualTo("Notation Declared");
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains("'png'");
        await Assert.That(issues[0].Message).Contains("'fmt'");
        await Assert.That(issues[0].Message).Contains("'r'");
    }

    [Test]
    public async Task ListedNotation_EveryUndeclaredNotationOfTheListIsReported()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("fmt", "gif", "png", "jpeg"))
            .Notation("png")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'gif'");
        await Assert.That(issues[1].Message).Contains("'jpeg'");
    }

    [Test]
    public async Task ListedNotation_OfAnAttributeListWithoutAnElementDeclaration_IsStillChecked()
    {
        var model = new NotationModel().Attlist("ghost", NotationAttribute("fmt", "gif")).Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
    }

    // Unique Notation Name

    [Test]
    public async Task UniqueNotationName_SecondDeclarationOfANotation_ReportsXml5026_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel().Notation("gif").Notation("gif").Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5026.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("Unique Notation Name");
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5026.Message);
        await Assert.That(issues[0].Message).Contains("'gif'");
    }

    [Test]
    public async Task UniqueNotationName_ThreeDeclarationsOfOneNotation_ReportEachDeclarationBeyondTheFirst()
    {
        var model = new NotationModel().Notation("gif").Notation("png").Notation("gif").Notation("gif").Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'gif'");
        await Assert.That(issues[1].Message).Contains("'gif'");
    }

    [Test]
    public async Task UniqueNotationName_DistinctNames_ReportNothing()
    {
        var model = new NotationModel().Notation("gif").Notation("png").Notation("GIF").Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task UniqueNotationName_SecondDeclarationInTheInternalSubset_ReportsXml5026_AndTheFirstBinds()
    {
        const string subset = "<!NOTATION gif SYSTEM \"first.exe\"><!NOTATION gif SYSTEM \"second.exe\">";
        var info = new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        var issues = Begin(declarations!).ToResult().Issues;

        await Assert.That(error).IsNull();
        await Assert.That(declarations!.Notations["gif"].SystemId).IsEqualTo("first.exe");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5026.Code);
    }

    [Test]
    public async Task UniqueNotationName_AViewThatIsNotTheParserModel_RecordsNoDuplicates_AndReportsNothing()
    {
        var issues = Begin(new Contracts.Fakes.FakeDtdDeclarations());

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    // The report order of BeginDocument is fixed

    [Test]
    public async Task BeginDocument_ReportsDuplicateNotationsThenEntitiesThenAttributeLists()
    {
        var model = new NotationModel()
            .Element("r", DtdContentKind.Empty)
            .Attlist("r", NotationAttribute("fmt", "missing"))
            .Entity(Unparsed("logo", "absent"))
            .Notation("gif")
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(4);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5026.Code);
        await Assert.That(issues[1].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
        await Assert.That(issues[1].Message).Contains("'logo'");
        await Assert.That(issues[2].Code).IsEqualTo(DtdValidityCodes.XML5017.Code);
        await Assert.That(issues[3].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
        await Assert.That(issues[3].Message).Contains("'missing'");
    }
}
