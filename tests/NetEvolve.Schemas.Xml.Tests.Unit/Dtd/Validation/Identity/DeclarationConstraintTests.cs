namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity.IdentityHarness;

/// <summary>
/// The declaration-time constraints One ID per Element Type and ID Attribute Default. Both are decided from the declarations
/// at <c>BeginDocument</c> and report at the document type declaration until per-declaration locations exist (#608).
/// </summary>
public class DeclarationConstraintTests
{
    [Test]
    public async Task OneIdPerElementType_TwoIdAttributes_ReportsXml5010AtTheDocumentTypeLocation()
    {
        var run = Run("", new Attlist("e", [Id("first"), Id("second")]));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5010.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("One ID per Element Type");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5010.Message);
        await Assert.That(issue.Message).Contains("'e'");
        await Assert.That(issue.Message).Contains("'second'");
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
    }

    [Test]
    public async Task OneIdPerElementType_ThreeIdAttributes_ReportsTheSecondAndTheThird()
    {
        var run = Run("", new Attlist("e", [Id("first"), Id("second"), Id("third")]));

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Message).Contains("'second'");
        await Assert.That(run.Issues[1].Message).Contains("'third'");
    }

    [Test]
    public async Task OneIdPerElementType_OneIdAttributeInEachElementType_IsValid()
    {
        var run = Run("", new Attlist("e", [Id("id")]), new Attlist("f", [Id("id")]));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task OneIdPerElementType_AttributesOfOtherTypes_DoNotCount()
    {
        var run = Run(
            "",
            new Attlist(
                "e",
                [
                    Id("id"),
                    Attribute("ref", DtdAttributeType.IdRef),
                    Attribute("refs", DtdAttributeType.IdRefs),
                    Attribute("name", DtdAttributeType.NmToken),
                ]
            )
        );

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task OneIdPerElementType_IsCheckedPerElementTypeInDeclarationOrder()
    {
        var run = Run(
            "",
            new Attlist("e", [Id("a"), Id("b")]),
            new Attlist("f", [Id("c")]),
            new Attlist("g", [Id("d"), Id("e")])
        );

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Message).Contains("'b'");
        await Assert.That(run.Issues[1].Message).Contains("'e'");
    }

    [Test]
    [Arguments(DtdDefaultKind.Implied)]
    [Arguments(DtdDefaultKind.Required)]
    public async Task IdAttributeDefault_ImpliedOrRequired_IsValid(DtdDefaultKind kind)
    {
        var run = Run("", new Attlist("e", [Id("id", kind)]));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments(DtdDefaultKind.Fixed)]
    [Arguments(DtdDefaultKind.Default)]
    public async Task IdAttributeDefault_FixedOrDefaultValue_ReportsXml5011AtTheDocumentTypeLocation(
        DtdDefaultKind kind
    )
    {
        var run = Run("", new Attlist("e", [Id("key", kind, "v")]));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5011.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("ID Attribute Default");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5011.Message);
        await Assert.That(issue.Message).Contains("'e'");
        await Assert.That(issue.Message).Contains("'key'");
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
    }

    [Test]
    [Arguments(DtdAttributeType.IdRef)]
    [Arguments(DtdAttributeType.IdRefs)]
    [Arguments(DtdAttributeType.Cdata)]
    [Arguments(DtdAttributeType.NmToken)]
    public async Task IdAttributeDefault_DefaultOnAnotherType_IsNotReported(DtdAttributeType type)
    {
        var run = Run("", new Attlist("e", [Attribute("a", type, DtdDefaultKind.Default, "v")]));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Declarations_BothConstraintsOnOneElementType_AreReportedOnceEach()
    {
        var run = Run("", new Attlist("e", [Id("first"), Id("second", DtdDefaultKind.Fixed, "v")]));

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5011.Code);
        await Assert.That(run.Issues[1].Code).IsEqualTo(DtdValidityCodes.XML5010.Code);
    }
}
