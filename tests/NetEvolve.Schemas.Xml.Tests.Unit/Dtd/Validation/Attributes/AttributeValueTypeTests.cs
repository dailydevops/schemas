namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>Validity constraint Attribute Value Type (XML 1.0, section 3.1): every attribute is declared and its value matches the declared type.</summary>
public class AttributeValueTypeTests
{
    private static DtdDeclarations Declared() =>
        new AttributeStepModel().Element("r").Element("c").Attlist("r", Cdata("a")).Build();

    [Test]
    public async Task AttributeValueType_ReportsXml5005_ForAnUndeclaredAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r a=\"1\" ~b=\"2\"/>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Attribute Value Type");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 10));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5005.Message);
        await Assert.That(issue.Message).Contains("'b'");
        await Assert.That(issue.Message).Contains("'r'");
    }

    [Test]
    public async Task UndeclaredAttribute_OnAnElementWithoutAnAttributeList_IsReported()
    {
        var run = Run("<r a=\"1\"><c ~x=\"1\"/></r>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'c'");
    }

    [Test]
    public async Task UndeclaredAttribute_OnAnUndeclaredElement_IsReported()
    {
        var run = Run("<ghost ~x=\"1\"/>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
    }

    [Test]
    public async Task UndeclaredAttributes_AreReportedOnceEach_InDocumentOrder()
    {
        var run = Run("<r ~x=\"1\" a=\"1\" y=\"2\"/>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'x'");
        await Assert.That(run.Issues[1].Message).Contains("'y'");
    }

    [Test]
    [Arguments("xml:lang")]
    [Arguments("xmlns")]
    [Arguments("xmlns:p")]
    public async Task ReservedAttributeNames_AreDeclaredLikeAnyOther(string name)
    {
        var run = Run("<r ~" + name + "=\"x\"/>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
        await Assert.That(run.Issues[0].Message).Contains("'" + name + "'");
    }

    [Test]
    public async Task DeclaredAttributes_ReportNothing()
    {
        var run = Run("<r a=\"1\"><c/></r>", Declared());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task AttributeNames_AreMatchedCaseSensitively()
    {
        var run = Run("<r ~A=\"1\"/>", Declared());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5005.Code);
    }

    [Test]
    public async Task DocumentWithoutADocumentTypeDeclaration_IsLeftToTheDocumentStep()
    {
        var run = Run("<r a=\"1\"/>", new AttributeStepModel(name: "").Build());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task CheckedValue_IsTheNormalizedOne_SoSurroundingSpacesInANameTokenAreValid()
    {
        // The reader normalizes by the declared type before the step sees the value (section 3.3.3).
        var read = AttributeHarness.Read(
            "<!DOCTYPE r [<!ELEMENT r EMPTY><!ATTLIST r t NMTOKEN #IMPLIED s NMTOKENS #IMPLIED>]><r t=\"  abc  \" s=\"  a   b  \"/>"
        );
        var model = new AttributeStepModel().Element("r").Attlist("r", NmToken("t"), NmTokens("s")).Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdAttributeStep();
        step.BeginDocument(Context(issues, model));
        var position = new XmlPosition(1, 1, 0);
        var attributes = new ValidationAttributeInfo[read.Sink.Infos.Count];
        for (var i = 0; i < attributes.Length; i++)
        {
            attributes[i] = new ValidationAttributeInfo(
                read.Sink.InfoNames[i].AsMemory(),
                read.Sink.InfoValues[i].AsMemory(),
                read.Sink.Infos[i].DeclaredType,
                read.Sink.Infos[i].IsSpecified,
                position
            );
        }

        step.StartElement(new ValidationElement("r".AsMemory(), position, attributes));

        await Assert.That(read.Code).IsNull();
        await Assert.That(read.Sink.InfoValues[0]).IsEqualTo("abc");
        await Assert.That(read.Sink.InfoValues[1]).IsEqualTo("a b");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultedAttribute_IsNotCheckedAgainstItsType_ItsDeclarationIsChecked()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Attlist("r", NmToken("t", DtdDefaultKind.Default, "bad value"))
            .Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdAttributeStep();
        step.BeginDocument(Context(issues, model));
        var afterDeclarations = issues.Count;

        step.StartElement(Element("r", "t", "bad value", DtdAttributeType.NmToken, isSpecified: false));

        await Assert.That(afterDeclarations).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
    }
}
