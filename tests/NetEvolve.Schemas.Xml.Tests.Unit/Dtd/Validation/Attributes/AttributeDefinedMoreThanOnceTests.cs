namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>The optional warning XML5903 for an attribute that is defined more than once (XML 1.0, section 3.3: the first definition binds).</summary>
public class AttributeDefinedMoreThanOnceTests
{
    [Test]
    public async Task SecondDefinitionOfAnAttribute_ReportsXml5903AsAWarning_AndTheFirstBinds()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Attlist("r", Cdata("a", DtdDefaultKind.Default, "first"), Cdata("a", DtdDefaultKind.Default, "second"))
            .Build();

        var issues = Begin(model).Issues;

        await Assert.That(model.AttributeLists["r"].Count).IsEqualTo(1);
        await Assert.That(model.AttributeLists["r"][0].DefaultValue).IsEqualTo("first");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5903.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5903.Message);
        await Assert.That(issues[0].Message).Contains("'a'");
        await Assert.That(issues[0].Message).Contains("'r'");
    }

    [Test]
    public async Task RedefinitionWithAnotherType_ReportsXml5903_AndDoesNotCheckTheDroppedDefinition()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Attlist("r", Cdata("a"), NmToken("a", DtdDefaultKind.Default, "x y"))
            .Build();

        var issues = Begin(model).Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5903.Code);
    }

    [Test]
    public async Task ThreeDefinitions_ReportEachDefinitionBeyondTheFirst_PerElementType()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Element("s")
            .Attlist("r", Cdata("a"), Cdata("a"), Cdata("b"), Cdata("a"))
            .Attlist("s", Cdata("a"))
            .Build();

        await Assert.That(Begin(model).Issues.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SameNameOnTwoElementTypes_IsNotRepeated()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Element("s")
            .Attlist("r", Cdata("a"))
            .Attlist("s", Cdata("a"))
            .Build();

        await Assert.That(Begin(model).Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InternalSubset_RepeatInOneAndAcrossAttlists_ReportsXml5903_AndTheFirstBinds()
    {
        const string subset =
            "<!ELEMENT r ANY><!ATTLIST r a CDATA \"one\" a CDATA \"two\"><!ATTLIST r a NMTOKEN \"three\" b CDATA #IMPLIED>";
        var info = new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        var issues = Begin(declarations!).Issues;

        await Assert.That(error).IsNull();
        await Assert.That(declarations!.AttributeLists["r"].Count).IsEqualTo(2);
        await Assert.That(declarations.AttributeLists["r"][0].DefaultValue).IsEqualTo("one");
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5903.Code);
        await Assert.That(issues[1].Code).IsEqualTo(DtdValidityCodes.XML5903.Code);
    }

    [Test]
    public async Task NoRepeatedAttributeDefinition_ReportsNothing()
    {
        var model = new AttributeStepModel().Element("r").Attlist("r", Cdata("a"), Cdata("b")).Build();

        await Assert.That(Begin(model).Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AViewThatIsNotTheParserModel_ReportsNothing() =>
        await Assert.That(Begin(new Contracts.Fakes.FakeDtdDeclarations()).Issues.Count).IsEqualTo(0);
}
