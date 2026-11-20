namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class XsiNilTests
{
    private const string Open = "<r " + XsiFixture.Prefixes;

    // r needs the child a and the attribute k.
    private static XsiFixture Required(bool nillable = true)
    {
        var schema = new XsiFixture();
        _ = schema.Global(
            "r",
            schema.Complex("required", SchemaFixture.Seq(XsiFixture.Child("a")), uses: schema.Use("k", true)),
            nillable
        );
        return schema;
    }

    private static XsiFixture Simple(
        bool nillable = true,
        ElementValueConstraintKind kind = ElementValueConstraintKind.None,
        string type = "string"
    )
    {
        var schema = new XsiFixture();
        _ = schema.Global("r", schema.Simple(type), nillable, new ElementValueConstraint(kind, "x"));
        return schema;
    }

    [Test]
    public async Task NilledElement_WithEmptyContent_IsValid_EvenWhenTheContentModelRequiresChildren()
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" xsi:nil=\"true\"/>", null, null, probe);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(probe.Nilled[0]).IsTrue();
        await Assert.That(probe.Valid[0]).IsTrue();
    }

    [Test]
    [Arguments("1")]
    [Arguments("&#x20;true&#xA;")]
    public async Task NilValue_IsCollapsed_AndOneMeansTrue(string value)
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" xsi:nil=\"" + value + "\"/>", null, null, probe);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(probe.Nilled[0]).IsTrue();
    }

    [Test]
    [Arguments("false")]
    [Arguments("0")]
    public async Task NilFalse_LeavesTheElementToBeAssessedAsUsual(string value)
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" xsi:nil=\"" + value + "\"/>", null, null, probe);

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 1, "'a'");
        await Assert.That(probe.Nilled[0]).IsFalse();
    }

    [Test]
    public async Task NilledSimpleElement_WithEmptyContent_IsValid_AndItsEmptyValueIsNotAssessed()
    {
        var probe = new ElementProbe();

        var nilled = XsiHarness.Run(Simple(type: "int"), Open + " xsi:nil=\"true\"/>", null, null, probe);
        var empty = XsiHarness.Run(Simple(type: "int"), Open + "/>");

        await Assert.That(nilled.Count).IsEqualTo(0);
        await Assert.That(probe.SimpleValues).IsEqualTo(0);
        await XsiHarness.AssertIssue(empty, "cvc-datatype-valid", null, 1);
    }

    [Test]
    [Arguments("x")]
    [Arguments(" ")]
    [Arguments("x<![CDATA[y]]>z")]
    public async Task NilledElement_WithCharacterData_IsInvalid_AndReportedOnce(string content)
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(
            Required(),
            Open + " k=\"1\" xsi:nil=\"true\">" + content + "</r>",
            null,
            null,
            probe
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 1, "'r'");
        await Assert.That(probe.Nilled[0]).IsTrue();
        await Assert.That(probe.Valid[0]).IsFalse();
    }

    [Test]
    public async Task NilledElement_WithAChild_IsInvalid_AndTheChildIsNotAssessed()
    {
        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" xsi:nil=\"true\"\n><a><z/></a>\n<a/></r>", null);

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 2, "'a'");
    }

    [Test]
    public async Task NilledElement_WithCharacterDataBeforeAChild_ReportsItsContentOnce()
    {
        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" xsi:nil=\"true\">x<a/>y</r>");

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 1, "character data");
    }

    [Test]
    public async Task NilledElement_OfAnEmptyContentType_ReportsCharacterDataAsNilledContent()
    {
        var schema = new XsiFixture();
        _ = schema.Global("r", schema.Complex("empty"), true);

        var issues = XsiHarness.Run(schema, Open + " xsi:nil=\"true\">x</r>");

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 1);
    }

    [Test]
    public async Task NilledElement_OfAMixedContentType_ReportsCharacterDataAsNilledContent()
    {
        var schema = new XsiFixture();
        _ = schema.Global("r", schema.Complex("mixed", SchemaFixture.Seq(XsiFixture.Child("a")), mixed: true), true);

        var issues = XsiHarness.Run(schema, Open + " xsi:nil=\"true\">x</r>");

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 1);
    }

    [Test]
    [Arguments("true")]
    [Arguments("false")]
    public async Task Nil_OnANonNillableElement_IsInvalid(string value)
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(Simple(false), Open + " xsi:nil=\"" + value + "\">x</r>", null, null, probe);

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.1", 1, "'r'");
        await Assert.That(probe.Nilled[0]).IsFalse();
    }

    [Test]
    public async Task NilTrue_OnAnElementWithAFixedValue_IsInvalid()
    {
        var issues = XsiHarness.Run(Simple(kind: ElementValueConstraintKind.Fixed), Open + " xsi:nil=\"true\"/>");

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.2", 1, "fixed");
    }

    [Test]
    public async Task NilFalse_OnAnElementWithAFixedValue_TakesTheFixedValue()
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(
            Simple(kind: ElementValueConstraintKind.Fixed),
            Open + " xsi:nil=\"false\"/>",
            null,
            null,
            probe
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(probe.SimpleValues).IsEqualTo(1);
    }

    [Test]
    public async Task NilTrue_OnAnElementWithADefaultValue_DoesNotApplyTheDefault()
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(
            Simple(kind: ElementValueConstraintKind.Default),
            Open + " xsi:nil=\"true\"/>",
            null,
            null,
            probe
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(probe.SimpleValues).IsEqualTo(0);
    }

    [Test]
    [Arguments("maybe")]
    [Arguments("")]
    [Arguments("TRUE")]
    public async Task NilValue_ThatIsNoBoolean_IsInvalid_AndTheElementIsNotNilled(string value)
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(Simple(), Open + " xsi:nil=\"" + value + "\">x</r>", null, null, probe);

        await XsiHarness.AssertIssue(issues, "cvc-datatype-valid", null, 1, "xsi:nil");
        await Assert.That(probe.Nilled[0]).IsFalse();
    }

    [Test]
    public async Task NilledElement_StillRequiresItsRequiredAttributes()
    {
        var issues = XsiHarness.Run(Required(), Open + " xsi:nil=\"true\"/>");

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.4", 1, "'k'");
    }

    [Test]
    public async Task NilledElement_StillRejectsAnUndeclaredAttribute()
    {
        var issues = XsiHarness.Run(Required(), Open + " k=\"1\" z=\"2\" xsi:nil=\"true\"/>");

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.3.2.1", 1, "'z'");
    }

    [Test]
    public async Task NilledElement_StillAssessesTheValueOfItsAttributes()
    {
        var schema = new XsiFixture();
        _ = schema.Global("r", schema.Complex("typed", uses: schema.Use("n", type: "int")), true);

        var issues = XsiHarness.Run(schema, Open + " n=\"x\" xsi:nil=\"true\"/>");

        await XsiHarness.AssertIssue(issues, "cvc-datatype-valid", null, 1, "'n'");
    }

    [Test]
    public async Task NilledSimpleElement_StillRejectsAnAttribute()
    {
        var issues = XsiHarness.Run(Simple(), Open + " z=\"1\" xsi:nil=\"true\"/>");

        await XsiHarness.AssertIssue(issues, "cvc-type", "cvc-type.3.1.1", 1, "'z'");
    }

    [Test]
    public async Task NilledElement_UsesTheAttributesOfItsXsiType()
    {
        var schema = new XsiFixture();
        var @base = schema.Complex("base");
        var derived = schema.Complex("derived", uses: schema.Use("k", true));
        _ = schema.Global("r", @base, true);
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var missing = XsiHarness.Run(schema, Open + " xsi:type=\"t:derived\" xsi:nil=\"true\"/>", check);
        var given = XsiHarness.Run(schema, Open + " k=\"1\" xsi:type=\"t:derived\" xsi:nil=\"true\"/>", check);

        await XsiHarness.AssertIssue(missing, "cvc-complex-type", "cvc-complex-type.4", 1, "'k'");
        await Assert.That(given.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Nil_OnAnElementWithoutADeclaration_IsNotAssessed()
    {
        var schema = new XsiFixture();
        _ = schema.Global(
            "r",
            schema.Complex(
                "root",
                SchemaFixture.Seq(SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax)))
            )
        );
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(schema, Open + "><z xsi:nil=\"true\">x</z></r>", null, null, probe);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(probe.Nilled[0]).IsFalse();
    }

    [Test]
    public async Task AttributeOfAnotherNamespace_IsNotTakenForXsiNil()
    {
        var probe = new ElementProbe();

        var issues = XsiHarness.Run(
            Required(),
            Open + " xmlns:q=\"urn:q\" k=\"1\" q:nil=\"true\"><a>x</a></r>",
            null,
            null,
            probe
        );

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.3.2.1", 1, "'{urn:q}nil'");
        await Assert.That(probe.Nilled[0]).IsFalse();
    }

    [Test]
    public async Task NilledChild_ThatIsInvalid_MakesItsParentInvalid_AndAValidOneDoesNot()
    {
        var schema = new XsiFixture();
        var item = schema.Complex("item");
        _ = schema.Global(
            "r",
            schema.Complex("root", SchemaFixture.Seq(XsiFixture.NillableChild("c", item, 0, long.MaxValue)))
        );
        var probe = new ElementProbe();
        var valid = new ElementProbe();

        var issues = XsiHarness.Run(
            schema,
            Open + ">\n<c xsi:nil=\"true\"/>\n<c xsi:nil=\"true\">x</c></r>",
            null,
            null,
            probe
        );
        var none = XsiHarness.Run(schema, Open + "><c xsi:nil=\"true\"/></r>", null, null, valid);

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.3.2.1", 3);
        await Assert.That(probe.Nilled.ToArray()).IsEquivalentTo([true, true, false]);
        await Assert.That(probe.Valid.ToArray()).IsEquivalentTo([true, false, false]);
        await Assert.That(none.Count).IsEqualTo(0);
        await Assert.That(valid.Valid.ToArray()).IsEquivalentTo([true, true]);
    }
}
