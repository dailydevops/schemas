namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationModel;

/// <summary>The optional warning XML5904 for an entity that is declared more than once (XML 1.0, section 4.2: the first declaration binds).</summary>
public class EntityDeclaredMoreThanOnceTests
{
    [Test]
    public async Task SecondDeclarationOfAGeneralEntity_ReportsXml5904AsAWarning_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel().Entity(Parsed("copy")).Entity(Parsed("copy")).Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5904.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5904.Message);
        await Assert.That(issues[0].Message).Contains("general entity 'copy'");
    }

    [Test]
    public async Task SecondDeclarationOfAParameterEntity_ReportsXml5904AsAWarning()
    {
        var model = new NotationModel().Entity(Parameter("pe")).Entity(Parameter("pe")).Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5904.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issues[0].Message).Contains("parameter entity 'pe'");
    }

    [Test]
    public async Task GeneralAndParameterEntityOfOneName_AreDistinct_AndReportNothing()
    {
        var model = new NotationModel().Entity(Parsed("x")).Entity(Parameter("x")).Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task ThreeDeclarations_ReportEachDeclarationBeyondTheFirst()
    {
        var model = new NotationModel().Entity(Parsed("a")).Entity(Parsed("a")).Entity(Parsed("b")).Entity(Parsed("a")).Build();

        await Assert.That(Begin(model).Count).IsEqualTo(2);
    }

    [Test]
    public async Task InternalSubset_SecondDeclarationReportsXml5904_AndTheFirstBinds()
    {
        const string subset = "<!ENTITY a \"first\"><!ENTITY a \"second\"><!ENTITY % p \"one\"><!ENTITY % p \"two\">";
        var info = new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        var issues = Begin(declarations!).ToResult().Issues;

        await Assert.That(error).IsNull();
        await Assert.That(declarations!.GeneralEntities["a"].Value).IsEqualTo("first");
        await Assert.That(declarations.ParameterEntities["p"].Value).IsEqualTo("one");
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5904.Code);
        await Assert.That(issues[1].Code).IsEqualTo(DtdValidityCodes.XML5904.Code);
    }

    [Test]
    public async Task NoRepeatedEntityDeclaration_ReportsNothing()
    {
        var model = new NotationModel().Entity(Parsed("a")).Entity(Parsed("b")).Entity(Parameter("p")).Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task AViewThatIsNotTheParserModel_ReportsNothing()
    {
        await Assert.That(Begin(new Contracts.Fakes.FakeDtdDeclarations()).Count).IsEqualTo(0);
    }
}
