namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document.DocumentHarness;

/// <summary>
/// Validity constraint Standalone Document Declaration (XML 1.0, section 2.9): a document with <c>standalone="yes"</c> must
/// not depend on a declaration of the external subset or of an external parameter entity. One test group per clause, each
/// with the valid counterpart; all four clauses report <c>XML5003</c> and name the clause.
/// </summary>
public class StandaloneDocumentDeclarationTests
{
    private static async Task AssertStandalone(ValidationIssue issue, XmlStandaloneClause clause, DocumentLocation at)
    {
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5003.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Standalone Document Declaration");
        await Assert.That(issue.Message).Contains("clause: " + clause);
        await Assert.That(issue.Location).IsEqualTo(at);
    }

    // Clause 1: attributes with default values declared externally, absent from the element in the document.

    private static DtdDeclarations DefaultModel(bool external, DtdDefaultKind kind, DtdAttributeType type) =>
        Declare(
            "doc",
            [Element("doc")],
            [
                (
                    "doc",
                    Attribute(
                        "a",
                        type,
                        kind,
                        kind is DtdDefaultKind.Default or DtdDefaultKind.Fixed ? "x" : null,
                        external
                    )
                ),
            ]
        );

    [Test]
    [Arguments(DtdDefaultKind.Default)]
    [Arguments(DtdDefaultKind.Fixed)]
    public async Task Defaults_ExternalDefaultThatTheDocumentRelaysOn_ReportsXml5003(DtdDefaultKind kind)
    {
        var session = new Session(DefaultModel(true, kind, DtdAttributeType.Cdata), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x", DtdAttributeType.Cdata, specified: false)
        );

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await AssertStandalone(session.Result[0], XmlStandaloneClause.Defaults, new DocumentLocation(SystemId, 3, 1));
        await Assert.That(session.Result[0].Message).Contains("'a'");
    }

    [Test]
    public async Task Defaults_AttributeSpecifiedInTheDocument_ReportsNothing()
    {
        var session = new Session(
            DefaultModel(true, DtdDefaultKind.Default, DtdAttributeType.Cdata),
            standalone: true
        ).Start("doc", At(3, 1), Attr("a", "y", DtdAttributeType.Cdata, specified: true));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Defaults_DefaultDeclaredInTheInternalSubset_ReportsNothing()
    {
        var session = new Session(
            DefaultModel(false, DtdDefaultKind.Default, DtdAttributeType.Cdata),
            standalone: true
        ).Start("doc", At(3, 1), Attr("a", "x", DtdAttributeType.Cdata, specified: false));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Defaults_WithoutStandaloneYes_ReportsNothing()
    {
        var session = new Session(
            DefaultModel(true, DtdDefaultKind.Default, DtdAttributeType.Cdata),
            standalone: false
        ).Start("doc", At(3, 1), Attr("a", "x", DtdAttributeType.Cdata, specified: false));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // Clause 2: entities declared externally and referenced.

    private static DtdDeclarations EntityModel(string name, bool external) =>
        Declare("doc", [Element("doc")], entities: [Entity(name, external)]);

    [Test]
    [Arguments(DtdEntityOutcome.DeclaredInternal)]
    [Arguments(DtdEntityOutcome.DeclaredExternal)]
    public async Task Entities_ReferenceToAnEntityDeclaredExternally_ReportsXml5003AtTheReference(
        DtdEntityOutcome outcome
    )
    {
        var session = new Session(EntityModel("e", external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Reference("e", outcome, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await AssertStandalone(session.Result[0], XmlStandaloneClause.Entities, new DocumentLocation(SystemId, 3, 6));
        await Assert.That(session.Result[0].Message).Contains("'e'");
    }

    [Test]
    public async Task Entities_ReferenceToAnEntityDeclaredInTheInternalSubset_ReportsNothing()
    {
        var session = new Session(EntityModel("e", external: false), standalone: true)
            .Start("doc", At(3, 1))
            .Reference("e", DtdEntityOutcome.DeclaredInternal, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Entities_WithoutStandaloneYes_ReportsNothing()
    {
        var session = new Session(EntityModel("e", external: true), standalone: false)
            .Start("doc", At(3, 1))
            .Reference("e", DtdEntityOutcome.DeclaredInternal, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("amp")]
    [Arguments("lt")]
    [Arguments("gt")]
    [Arguments("apos")]
    [Arguments("quot")]
    public async Task Entities_PredefinedEntityDeclaredExternally_IsNotAViolation(string name)
    {
        var session = new Session(EntityModel(name, external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Reference(name, DtdEntityOutcome.DeclaredInternal, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    // Clause 3: attributes with tokenized types declared externally whose value normalization changes.

    private static DtdDeclarations TypeModel(bool external, DtdAttributeType type) =>
        Declare("doc", [Element("doc")], [("doc", Attribute("a", type, DtdDefaultKind.Implied, null, external))]);

    [Test]
    [Arguments(DtdAttributeType.NmToken)]
    [Arguments(DtdAttributeType.NmTokens)]
    [Arguments(DtdAttributeType.Id)]
    [Arguments(DtdAttributeType.IdRef)]
    [Arguments(DtdAttributeType.IdRefs)]
    [Arguments(DtdAttributeType.Entity)]
    [Arguments(DtdAttributeType.Entities)]
    public async Task Normalization_TokenizedTypeDeclaredExternally_WhoseValueChanges_ReportsXml5003AtTheAttribute(
        DtdAttributeType type
    )
    {
        var session = new Session(TypeModel(true, type), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x y", type, changed: true, line: 3, column: 6)
        );

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await AssertStandalone(
            session.Result[0],
            XmlStandaloneClause.Normalization,
            new DocumentLocation(SystemId, 3, 6)
        );
        await Assert.That(session.Result[0].Message).Contains("'a'");
    }

    [Test]
    public async Task Normalization_ValueThatNormalizationLeavesAsItIs_ReportsNothing()
    {
        var session = new Session(TypeModel(true, DtdAttributeType.NmToken), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x", DtdAttributeType.NmToken, changed: false)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Normalization_TypeDeclaredInTheInternalSubset_ReportsNothing()
    {
        var session = new Session(TypeModel(false, DtdAttributeType.NmToken), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x y", DtdAttributeType.NmToken, changed: true)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Normalization_CdataType_IsNotATokenizedType()
    {
        var session = new Session(TypeModel(true, DtdAttributeType.Cdata), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x y", DtdAttributeType.Cdata, changed: true)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(DtdAttributeType.Enumeration)]
    [Arguments(DtdAttributeType.Notation)]
    public async Task Normalization_EnumeratedType_IsNotATokenizedType_AsSection29SaysTokenized(DtdAttributeType type)
    {
        var session = new Session(TypeModel(true, type), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x", type, changed: true)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Normalization_WithoutStandaloneYes_ReportsNothing()
    {
        var session = new Session(TypeModel(true, DtdAttributeType.NmToken), standalone: false).Start(
            "doc",
            At(3, 1),
            Attr("a", "x y", DtdAttributeType.NmToken, changed: true)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Normalization_AttributeOfAnElementWithoutAnAttributeList_ReportsNothing()
    {
        var session = new Session(Declare("doc", [Element("doc")]), standalone: true).Start(
            "doc",
            At(3, 1),
            Attr("a", "x y", DtdAttributeType.NmToken, changed: true)
        );

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task IsChangedByNormalization_DefaultsToFalse_AndPublicConstructorDoesNotSetIt()
    {
        var info = new ValidationAttributeInfo(
            "a".AsMemory(),
            "x".AsMemory(),
            DtdAttributeType.NmToken,
            true,
            At(1, 1)
        );

        await Assert.That(info.IsChangedByNormalization).IsFalse();
    }

    // Clause 4: elements with element content declared externally that contain white space.

    private static DtdDeclarations ContentModel(bool external, bool mixed = false) =>
        Declare("doc", [mixed ? Mixed("doc", "item") : Children("doc", external, "item"), Element("item")]);

    [Test]
    public async Task WhiteSpace_InElementContentDeclaredExternally_ReportsXml5003AtTheWhiteSpace()
    {
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Text("  ", XmlTextKind.Text, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await AssertStandalone(session.Result[0], XmlStandaloneClause.WhiteSpace, new DocumentLocation(SystemId, 3, 6));
        await Assert.That(session.Result[0].Message).Contains("'doc'");
    }

    [Test]
    public async Task WhiteSpace_LineBreakReportedAsAReference_IsWhiteSpace()
    {
        // The reader reports every line break as one reference with the text "\n" (limitation of the content model step).
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Text("\n", XmlTextKind.Reference, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await AssertStandalone(session.Result[0], XmlStandaloneClause.WhiteSpace, new DocumentLocation(SystemId, 3, 6));
    }

    [Test]
    public async Task WhiteSpace_IsReportedOncePerInstance_AndAgainForTheNextInstance()
    {
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(1, 1))
            .Text(" ", XmlTextKind.Text, At(1, 6))
            .Start("item", At(1, 7))
            .End("item")
            .Text(" ", XmlTextKind.Text, At(1, 20))
            .End("doc")
            .Start("doc", At(2, 1))
            .Text(" ", XmlTextKind.Text, At(2, 6));

        await Assert.That(session.Result.Count).IsEqualTo(2);
        await Assert.That(session.Result[0].Location).IsEqualTo(new DocumentLocation(SystemId, 1, 6));
        await Assert.That(session.Result[1].Location).IsEqualTo(new DocumentLocation(SystemId, 2, 6));
    }

    [Test]
    public async Task WhiteSpace_InAChildOfTheElement_IsJudgedByTheChildsDeclaration()
    {
        // item has no element content, so its white space is fine; doc is checked again after item ends.
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(1, 1))
            .Start("item", At(1, 6))
            .Text(" ", XmlTextKind.Text, At(1, 12))
            .End("item");

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WhiteSpace_InElementContentDeclaredInTheInternalSubset_ReportsNothing()
    {
        var session = new Session(ContentModel(external: false), standalone: true)
            .Start("doc", At(3, 1))
            .Text("  ", XmlTextKind.Text, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WhiteSpace_InMixedContentDeclaredExternally_ReportsNothing()
    {
        var model = Declare("doc", [new DtdElementDeclaration("doc", DtdContentKind.Mixed, null, true)]);
        var session = new Session(model, standalone: true)
            .Start("doc", At(3, 1))
            .Text("  ", XmlTextKind.Text, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WhiteSpace_WithoutStandaloneYes_ReportsNothing()
    {
        var session = new Session(ContentModel(external: true), standalone: false)
            .Start("doc", At(3, 1))
            .Text("  ", XmlTextKind.Text, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WhiteSpace_CdataSectionAndCharacterReference_AreCharacterDataNotWhiteSpace()
    {
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Text("  ", XmlTextKind.CData, At(3, 6))
            .Text(" ", XmlTextKind.Reference, At(3, 12));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WhiteSpace_TextWithoutWhiteSpace_ReportsNothing()
    {
        var session = new Session(ContentModel(external: true), standalone: true)
            .Start("doc", At(3, 1))
            .Text("x", XmlTextKind.Text, At(3, 6));

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }
}
