namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>
/// Validity constraints Attribute Default Value Syntactically Correct (section 3.3.2) and No Duplicate Tokens (section 3.3.1),
/// and the warning for attributes of an undeclared element type: all three are properties of the declarations, so they are
/// reported at the document type declaration whether or not an element uses the attribute.
/// </summary>
public class DeclarationConstraintTests
{
    private static DtdDeclarations One(DtdAttributeDefinition definition) =>
        new AttributeStepModel()
            .Element("r")
            .Entity("ok", "abc")
            .Entity("pair", "a b")
            .Entity("deep", "&ok;")
            .Attlist("r", definition)
            .Build();

    [Test]
    public async Task DefaultValue_ThatViolatesItsType_ReportsXml5021_AtTheDeclaration_AlsoWhenNeverUsed()
    {
        var run = Begin(One(NmToken("n", DtdDefaultKind.Default, "a b")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Attribute Default Value Syntactically Correct");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5021.Message);
        await Assert.That(issue.Message).Contains("'n'");
        await Assert.That(issue.Message).Contains("'r'");
        await Assert.That(issue.Message).Contains("'a b'");
    }

    [Test]
    public async Task DefaultValue_ThatViolatesItsType_IsReportedOnceAtTheDeclaration_NotAtTheElement()
    {
        var run = Run("<r/><r/>", One(NmToken("n", DtdDefaultKind.Fixed, "a b")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(DoctypeLocation);
    }

    [Test]
    [Arguments(DtdAttributeType.Cdata, "anything at all!")]
    [Arguments(DtdAttributeType.NmToken, "abc")]
    [Arguments(DtdAttributeType.NmToken, "  abc  ")]
    [Arguments(DtdAttributeType.NmToken, "1")]
    [Arguments(DtdAttributeType.NmToken, "&#65;")]
    [Arguments(DtdAttributeType.NmToken, "&ok;")]
    [Arguments(DtdAttributeType.NmToken, "&deep;")]
    [Arguments(DtdAttributeType.NmToken, "a\U00010000")]
    [Arguments(DtdAttributeType.NmTokens, "a b")]
    [Arguments(DtdAttributeType.NmTokens, "  a   b  ")]
    [Arguments(DtdAttributeType.NmTokens, "a\tb")]
    [Arguments(DtdAttributeType.NmTokens, "a\nb")]
    [Arguments(DtdAttributeType.NmTokens, "a\rb")]
    [Arguments(DtdAttributeType.NmTokens, "a\r\nb")]
    [Arguments(DtdAttributeType.NmTokens, "&pair;")]
    [Arguments(DtdAttributeType.NmTokens, "&ok; &deep;")]
    [Arguments(DtdAttributeType.IdRef, "a1")]
    [Arguments(DtdAttributeType.IdRefs, "a1 b2")]
    [Arguments(DtdAttributeType.Entity, "a")]
    [Arguments(DtdAttributeType.Entities, "a b")]
    [Arguments(DtdAttributeType.Id, "a1")]
    public async Task DefaultValue_ThatMatchesItsType_ReportsNothing(DtdAttributeType type, string value)
    {
        var run = Begin(One(Attr("x", type, DtdDefaultKind.Default, value)));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments(DtdAttributeType.NmToken, "a b")]
    [Arguments(DtdAttributeType.NmToken, "a!")]
    [Arguments(DtdAttributeType.NmToken, "")]
    [Arguments(DtdAttributeType.NmToken, "   ")]
    [Arguments(DtdAttributeType.NmToken, "&#x20;")]
    [Arguments(DtdAttributeType.NmToken, "&#60;")]
    [Arguments(DtdAttributeType.NmToken, "&amp;")]
    [Arguments(DtdAttributeType.NmToken, "&#9;a")]
    [Arguments(DtdAttributeType.NmToken, "&pair;")]
    [Arguments(DtdAttributeType.NmTokens, "a b!")]
    [Arguments(DtdAttributeType.NmTokens, "")]
    [Arguments(DtdAttributeType.NmTokens, "a&#9;b")]
    [Arguments(DtdAttributeType.IdRef, "1a")]
    [Arguments(DtdAttributeType.IdRef, "a b")]
    [Arguments(DtdAttributeType.IdRefs, "a 1")]
    [Arguments(DtdAttributeType.Entity, "-a")]
    [Arguments(DtdAttributeType.Entities, "a -b")]
    [Arguments(DtdAttributeType.Id, "1")]
    public async Task DefaultValue_ThatViolatesItsType_IsReported(DtdAttributeType type, string value)
    {
        var run = Begin(One(Attr("x", type, DtdDefaultKind.Default, value)));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(DoctypeLocation);
    }

    [Test]
    [Arguments(DtdDefaultKind.Default)]
    [Arguments(DtdDefaultKind.Fixed)]
    public async Task DefaultValue_OfAnEnumeration_MustBeADeclaredToken(DtdDefaultKind kind)
    {
        var good = Begin(One(Enumeration("e", kind, "  b ", "a", "b")));
        var bad = Begin(One(Enumeration("e", kind, "c", "a", "b")));

        await Assert.That(good.Issues).IsEmpty();
        await Assert.That(bad.Issues.Count).IsEqualTo(1);
        await Assert.That(bad.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
    }

    [Test]
    public async Task DefaultValue_OfANotationType_MustBeAListedNotation()
    {
        var good = Begin(One(Attr("n", DtdAttributeType.Notation, DtdDefaultKind.Default, "gif", "gif", "png")));
        var bad = Begin(One(Attr("n", DtdAttributeType.Notation, DtdDefaultKind.Default, "bmp", "gif", "png")));

        await Assert.That(good.Issues).IsEmpty();
        await Assert.That(bad.Issues.Count).IsEqualTo(1);
        await Assert.That(bad.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
    }

    [Test]
    [Arguments(DtdDefaultKind.Implied)]
    [Arguments(DtdDefaultKind.Required)]
    public async Task AttributeWithoutADefault_HasNothingToCheck(DtdDefaultKind kind)
    {
        var run = Begin(One(Attr("n", DtdAttributeType.NmToken, kind)));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("&ghost;")]
    [Arguments("&logo;")]
    [Arguments("&ext;")]
    [Arguments("a<b")]
    [Arguments("&#0;")]
    [Arguments("&bad")]
    [Arguments("&loop1;")]
    public async Task DefaultValue_ThatCannotBeExpanded_IsLeftToTheReader(string value)
    {
        // Undeclared, unparsed, and external entities, a literal '<', an illegal reference, and a recursion are the
        // well-formedness errors of the reader; the step reports no second, misleading validity error for them.
        var model = new AttributeStepModel()
            .Element("r")
            .Unparsed("logo")
            .External("ext")
            .Entity("loop1", "&loop2;")
            .Entity("loop2", "&loop1;")
            .Attlist("r", NmToken("n", DtdDefaultKind.Default, value))
            .Build();

        var run = Begin(model);

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task DefaultValue_BeyondTheEntityDepthLimit_IsLeftToTheReader()
    {
        // "deep" holds a reference to "ok": the expansion is two levels deep.
        var model = One(NmToken("n", DtdDefaultKind.Default, "a b&deep;"));

        var within = Begin(model, XmlReaderLimits.Default.WithMaxEntityDepth(2));
        var beyond = Begin(model, XmlReaderLimits.Default.WithMaxEntityDepth(1));

        await Assert.That(within.Issues.Count).IsEqualTo(1);
        await Assert.That(beyond.Issues).IsEmpty();
    }

    [Test]
    public async Task DefaultValue_BeyondTheAttributeValueLengthLimit_IsLeftToTheReader()
    {
        var model = One(NmToken("n", DtdDefaultKind.Default, "abcd efgh"));

        var within = Begin(model, XmlReaderLimits.Default.WithMaxAttributeValueLength(9));
        var beyond = Begin(model, XmlReaderLimits.Default.WithMaxAttributeValueLength(8));
        var farBeyond = Begin(model, XmlReaderLimits.Default.WithMaxAttributeValueLength(4));

        await Assert.That(within.Issues.Count).IsEqualTo(1);
        await Assert.That(beyond.Issues).IsEmpty();
        await Assert.That(farBeyond.Issues).IsEmpty();
    }

    [Test]
    public async Task NoDuplicateTokens_ReportsXml5018_AtTheDeclaration_WithCodeSeverityConstraintAndLocation()
    {
        var run = Begin(One(Enumeration("e", "a", "b", "a")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5018.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("No Duplicate Tokens");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5018.Message);
        await Assert.That(issue.Message).Contains("'a'");
        await Assert.That(issue.Message).Contains("'e'");
        await Assert.That(issue.Message).Contains("'r'");
    }

    [Test]
    public async Task NoDuplicateTokens_AppliesToNotationTypes()
    {
        var run = Begin(One(Attr("n", DtdAttributeType.Notation, DtdDefaultKind.Implied, null, "gif", "png", "gif")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5018.Code);
        await Assert.That(run.Issues[0].Message).Contains("'gif'");
    }

    [Test]
    public async Task NoDuplicateTokens_ReportsEachRepeatedOccurrence()
    {
        var run = Begin(One(Enumeration("e", "a", "a", "b", "b", "a")));

        await Assert.That(run.Issues.Count).IsEqualTo(3);
    }

    [Test]
    public async Task NoDuplicateTokens_TokensThatDifferOnlyInCase_AreDistinct()
    {
        var run = Begin(One(Enumeration("e", "a", "A")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task CdataAttribute_HasNoTokensToCompare()
    {
        var run = Begin(One(Cdata("c")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task AttributeList_OfAnUndeclaredElementType_ReportsXml5902AsAWarning()
    {
        var model = new AttributeStepModel().Element("r").Attlist("ghost", Cdata("a")).Attlist("r", Cdata("b")).Build();

        var run = Begin(model);

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5902.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issue.Constraint).IsEqualTo("Attributes Declared For Undeclared Element Type");
        await Assert.That(issue.Location).IsEqualTo(DoctypeLocation);
        await Assert.That(issue.Message).Contains("'ghost'");
    }

    [Test]
    public async Task AttributeLists_OfDeclaredElementTypes_ReportNothing()
    {
        var run = Begin(new AttributeStepModel().Element("r").Attlist("r", Cdata("a")).Build());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task DeclarationChecks_OfADocumentWithoutADocumentTypeDeclaration_ReportNothing()
    {
        var model = new AttributeStepModel(name: "").Attlist("ghost", Enumeration("e", "a", "a")).Build();

        var run = Begin(model);

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task DeclarationChecks_ReportInDeclarationOrder_AndOnePerProblem()
    {
        var model = new AttributeStepModel()
            .Element("r")
            .Attlist("ghost", Cdata("a"))
            .Attlist(
                "r",
                Enumeration("e", DtdDefaultKind.Default, "z", "a", "a"),
                NmToken("n", DtdDefaultKind.Default, "x y")
            )
            .Build();

        var run = Begin(model);

        await Assert.That(run.Issues.Count).IsEqualTo(4);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5902.Code);
        await Assert.That(run.Issues[1].Code).IsEqualTo(DtdValidityCodes.XML5018.Code);
        await Assert.That(run.Issues[2].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
        await Assert.That(run.Issues[3].Code).IsEqualTo(DtdValidityCodes.XML5021.Code);
    }
}
