namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Notations;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationModel;

/// <summary>
/// Validity constraints Notation Attributes, One Notation Per Element Type, and No Notation on Empty Element
/// (XML 1.0, section 3.3.1).
/// </summary>
public class NotationAttributeConstraintTests
{
    private static DtdDeclarations Pictures() =>
        new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("fmt", "gif", "png"), CdataAttribute("c"))
            .Notation("gif")
            .Notation("png")
            .Notation("jpeg")
            .Build();

    // Notation Attributes: the value matches one of the notations of the list

    [Test]
    public async Task NotationAttributes_ValueOutsideTheList_ReportsXml5015_AtTheAttribute_WithCodeSeverityConstraintAndLocation()
    {
        var run = Run("<r ~fmt=\"tiff\"/>", Pictures());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5015.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Notation Attributes");
        await Assert.That(issue.Location).IsEqualTo(run.Expected);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5015.Message);
        await Assert.That(issue.Message).Contains("'tiff'");
    }

    [Test]
    [Arguments("gif")]
    [Arguments("png")]
    public async Task NotationAttribute_WithAValueOfTheList_ReportsNothing(string value)
    {
        var run = Run("<r fmt=\"" + value + "\"/>", Pictures());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task NotationAttribute_WithADeclaredNotationThatIsNotInItsList_IsReported()
    {
        var run = Run("<r ~fmt=\"jpeg\"/>", Pictures());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5015.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("GIF")]
    [Arguments("gif png")]
    [Arguments("gif ")]
    public async Task NotationAttribute_WithAValueThatIsNotExactlyOneListedName_IsReported(string value)
    {
        var run = Run("<r ~fmt=\"" + value + "\"/>", Pictures());

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5015.Code);
    }

    [Test]
    public async Task NotationAttributes_EachElementIsChecked_AgainstTheListOfItsOwnType()
    {
        var model = new NotationModel()
            .Element("r")
            .Element("s")
            .Attlist("r", NotationAttribute("fmt", "gif"))
            .Attlist("s", NotationAttribute("fmt", "png"))
            .Notation("gif")
            .Notation("png")
            .Build();

        var run = Run("<r fmt=\"gif\"><s ~fmt=\"gif\"/></r>", model);

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task NotationAttributes_TwoAttributesOfOneElementType_AreCheckedEachAgainstItsOwnList()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("a", "gif"), NotationAttribute("b", "png"))
            .Notation("gif")
            .Notation("png")
            .Build();

        var run = Run("<r a=\"gif\" ~b=\"gif\"/>", model);

        // The second NOTATION attribute is reported once at BeginDocument (One Notation Per Element Type); the value is a separate issue.
        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5016.Code);
        await Assert.That(run.Issues[1].Code).IsEqualTo(DtdValidityCodes.XML5015.Code);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task NotationAttributes_ListedNotationThatIsUndeclared_IsNotReportedAgainForEveryElement()
    {
        var model = new NotationModel().Element("r").Attlist("r", NotationAttribute("fmt", "gif")).Build();

        var run = Run("<r fmt=\"gif\"><r fmt=\"gif\"/><r fmt=\"gif\"/></r>", model);

        // One report at BeginDocument (see NotationDeclaredConstraintTests); no value issue per element.
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5025.Code);
    }

    [Test]
    public async Task NotationAttribute_DefaultedValueOutsideTheList_IsCheckedLikeASpecifiedOne()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();
        step.BeginDocument(Context(issues, Pictures()));

        step.StartElement(Element("r", "fmt", "tiff", DtdAttributeType.Notation, isSpecified: false));

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    // Notation declared after its use in an attribute list

    [Test]
    public async Task NotationDeclaredAfterItsUseInAnAttributeList_IsAccepted()
    {
        // The attribute list comes first and the notations after it; the model is complete before any check runs.
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("fmt", "gif", "png"))
            .Notation("gif")
            .Notation("png")
            .Build();

        var run = Run("<r fmt=\"png\"/>", model);

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task NotationDeclaredAfterItsUseInTheInternalSubset_IsAccepted()
    {
        const string subset =
            "<!ELEMENT r ANY><!ATTLIST r fmt NOTATION (gif|png) #IMPLIED>"
            + "<!NOTATION gif SYSTEM \"gif.exe\"><!NOTATION png SYSTEM \"png.exe\">";
        var info = new XmlDoctypeInfo("r".AsSpan(), default, default, subset.AsSpan());
        var error = DtdSubsetParser.Parse(in info, XmlReaderLimits.Default, out var declarations);

        var issues = Begin(declarations!);

        await Assert.That(error).IsNull();
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    // One Notation Per Element Type

    [Test]
    public async Task OneNotationPerElementType_TwoNotationAttributes_ReportsXml5016_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("a", "gif"), NotationAttribute("b", "gif"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5016.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("One Notation Per Element Type");
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5016.Message);
        await Assert.That(issues[0].Message).Contains("'r'");
        await Assert.That(issues[0].Message).Contains("'b'");
    }

    [Test]
    public async Task OneNotationPerElementType_ThreeNotationAttributes_ReportsEachBeyondTheFirst()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("a", "gif"), NotationAttribute("b", "gif"), NotationAttribute("c", "gif"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'b'");
        await Assert.That(issues[1].Message).Contains("'c'");
    }

    [Test]
    public async Task OneNotationPerElementType_OneNotationAttributeOnEachOfTwoElementTypes_ReportsNothing()
    {
        var model = new NotationModel()
            .Element("r")
            .Element("s")
            .Attlist("r", NotationAttribute("a", "gif"), CdataAttribute("b"))
            .Attlist("s", NotationAttribute("a", "gif"))
            .Notation("gif")
            .Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task OneNotationPerElementType_AttributeDeclaredTwice_FirstBinds_AndIsNotASecondNotationAttribute()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", NotationAttribute("a", "gif"), NotationAttribute("a", "gif"))
            .Notation("gif")
            .Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    // No Notation on Empty Element

    [Test]
    public async Task NoNotationOnEmptyElement_NotationAttributeOnEmptyElement_ReportsXml5017_AtTheDocumentTypeLocation()
    {
        var model = new NotationModel()
            .Element("r", DtdContentKind.Empty)
            .Attlist("r", NotationAttribute("fmt", "gif"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5017.Code);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo("No Notation on Empty Element");
        await Assert.That(issues[0].Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issues[0].Message).Contains(DtdValidityCodes.XML5017.Message);
        await Assert.That(issues[0].Message).Contains("'r'");
        await Assert.That(issues[0].Message).Contains("'fmt'");
    }

    [Test]
    [Arguments(DtdContentKind.Any)]
    [Arguments(DtdContentKind.Mixed)]
    [Arguments(DtdContentKind.Children)]
    public async Task NoNotationOnEmptyElement_NotationAttributeOnAnyOtherContentKind_ReportsNothing(
        DtdContentKind kind
    )
    {
        var model = new NotationModel()
            .Element("r", kind)
            .Attlist("r", NotationAttribute("fmt", "gif"))
            .Notation("gif")
            .Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoNotationOnEmptyElement_OtherAttributeTypesOnEmptyElement_ReportNothing()
    {
        var model = new NotationModel()
            .Element("r", DtdContentKind.Empty)
            .Attlist("r", CdataAttribute("a"), EntityAttribute("b"))
            .Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoNotationOnEmptyElement_AttributeListOfAnUndeclaredElementType_ReportsNothing()
    {
        var model = new NotationModel().Attlist("ghost", NotationAttribute("fmt", "gif")).Notation("gif").Build();

        await Assert.That(Begin(model).Count).IsEqualTo(0);
    }

    [Test]
    public async Task NoNotationOnEmptyElement_EachNotationAttributeIsReported_AlongsideOneNotationPerElementType()
    {
        var model = new NotationModel()
            .Element("r", DtdContentKind.Empty)
            .Attlist("r", NotationAttribute("a", "gif"), NotationAttribute("b", "gif"))
            .Notation("gif")
            .Build();

        var issues = Begin(model).ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5017.Code);
        await Assert.That(issues[1].Code).IsEqualTo(DtdValidityCodes.XML5016.Code);
        await Assert.That(issues[2].Code).IsEqualTo(DtdValidityCodes.XML5017.Code);
    }
}
