namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using System.Globalization;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity.IdentityHarness;

/// <summary>The validity constraint ID: the value is a Name and no other ID attribute of the document has it.</summary>
public class IdConstraintTests
{
    [Test]
    public async Task Id_UniqueValues_AreValid()
    {
        var run = Run("<r id=\"a\"><a id=\"b\"/><b id=\"c\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Id_Duplicate_ReportsXml5009AtTheSecondOccurrence()
    {
        var run = Run("<r id=\"x\">\n  <a ~id=\"x\"/>\n</r>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("ID");
        await Assert.That(issue.Location).IsEqualTo(run.Expected[0]);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 2, 6));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5009.Message);
        await Assert.That(issue.Message).Contains("'x'");
    }

    [Test]
    public async Task Id_Repeated_ReportsEveryRepeatAtItsOwnPosition()
    {
        var run = Run("<r id=\"x\"><a ~id=\"x\"/><b ~id=\"x\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected[1]);
    }

    [Test]
    public async Task Id_SameValueOnDifferentElementTypes_IsADuplicate()
    {
        var run = Run("<r><a id=\"x\"/><b ~id=\"x\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Id_ValuesThatDifferInCase_AreDistinct()
    {
        var run = Run("<r id=\"a\"><a id=\"A\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("1a")]
    [Arguments("a b")]
    [Arguments("")]
    [Arguments("-x")]
    [Arguments(".x")]
    [Arguments("a,b")]
    [Arguments("a#")]
    [Arguments(" a")]
    [Arguments("a ")]
    public async Task Id_ThatIsNotAName_ReportsXml5009AtTheAttribute(string value)
    {
        var run = Run("<r ~id=\"" + value + "\"/>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[0].Message).Contains("'" + value + "'");
        await Assert.That(run.Issues[0].Message).Contains("not a name");
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
    }

    [Test]
    [Arguments("a")]
    [Arguments("_")]
    [Arguments(":")]
    [Arguments("a1")]
    [Arguments("a-b.c")]
    [Arguments("a:b")]
    [Arguments("é")]
    [Arguments("名前")]
    [Arguments("\U00010000x")]
    public async Task Id_ThatIsAName_IsValid(string value)
    {
        var run = Run("<r id=\"" + value + "\"/>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Id_ThatIsNotAName_IsNotRegistered_SoARepeatIsNotAlsoADuplicate()
    {
        var run = Run("<r ~id=\"1a\"><a ~id=\"1a\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Message).Contains("not a name");
        await Assert.That(run.Issues[1].Message).Contains("not a name");
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected[1]);
    }

    [Test]
    public async Task Attribute_NotDeclaredAsId_IsIgnored()
    {
        var run = Run("<r note=\"x\" other=\"y\"><a note=\"x\" other=\"y\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Id_WithADefaultedValue_IsNotRegistered_SoTheInvalidDeclarationIsReportedOnce()
    {
        // The declaration is invalid (ID Attribute Default) and reported once at the start; the defaulted value that
        // every element receives does not add a duplicate report per element.
        var run = Run(
            "<r><a/><a/><a/></r>",
            new Attlist("r", [Attribute("code", DtdAttributeType.Cdata)]),
            new Attlist("a", [Id("id", DtdDefaultKind.Default, "d")])
        );

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5011.Code);
    }

    [Test]
    public async Task Id_SpecifiedValueEqualToADefaultOfAnotherElement_IsNotADuplicate()
    {
        var run = Run(
            "<r><a/><b id=\"d\"/></r>",
            new Attlist("a", [Id("id", DtdDefaultKind.Default, "d")]),
            new Attlist("b", [Id("id")])
        );

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5011.Code);
    }

    [Test]
    public async Task Id_ManyDistinctValues_AreAllAcceptedAndEveryRepeatIsDetected()
    {
        const int count = 3000;
        var issues = Collector(maxIssues: count * 2);
        var declarations = Declare(Standard("e"));
        var step = new DtdIdentityStep();
        step.BeginDocument(Context(issues, declarations));

        for (var pass = 0; pass < 2; pass++)
        {
            for (var n = 0; n < count; n++)
            {
                step.StartElement(Element("e", "i" + n.ToString(CultureInfo.InvariantCulture), n + 1));
            }

            await Assert.That(issues.Count).IsEqualTo(pass * count);
        }

        step.EndDocument();
        await Assert.That(issues.Count).IsEqualTo(count);
        await Assert.That(issues.ToResult().Issues[count - 1].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
    }

    private static ValidationElement Element(string name, string id, int line)
    {
        var attribute = new ValidationAttributeInfo(
            "id".AsMemory(),
            id.AsMemory(),
            DtdAttributeType.Id,
            isSpecified: true,
            new XmlPosition(line, 4, line * 10)
        );

        return new ValidationElement(name.AsMemory(), new XmlPosition(line, 1, line * 10), new[] { attribute });
    }
}
