namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class XsiTypeTests
{
    private const string Ext = "<r " + XsiFixture.Prefixes + " xsi:type=\"t:derived\"";

    // r is declared with the type base (a sequence of a); derived is a sequence of b.
    private static (XsiFixture Schema, ComplexTypeDefinition Base, ComplexTypeDefinition Derived) Types(
        XsdDerivationMethod elementBlock = XsdDerivationMethod.None,
        XsdDerivationMethod typeBlock = XsdDerivationMethod.None,
        bool abstractBase = false,
        bool abstractDerived = false
    )
    {
        var schema = new XsiFixture();
        var @base = schema.Complex("base", SchemaFixture.Seq(XsiFixture.Child("a")), abstractBase, typeBlock);
        var derived = schema.Complex("derived", SchemaFixture.Seq(XsiFixture.Child("b")), abstractDerived);
        _ = schema.Global("r", @base, block: elementBlock);
        return (schema, @base, derived);
    }

    [Test]
    public async Task DerivedXsiType_IsUsedForTheAssessment()
    {
        var (schema, @base, derived) = Types();
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DerivedXsiType_ReplacesTheContentModelOfTheDeclaredType()
    {
        var (schema, @base, derived) = Types();
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(schema, Ext + ">\n<a>x</a></r>", check);

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 2, "'a'");
    }

    [Test]
    public async Task DerivedXsiType_TheStartHookSeesTheChosenType()
    {
        var (schema, @base, derived) = Types();
        var check = new ScriptedDerivationCheck().Allow(derived, @base);
        var probe = new ElementProbe();

        _ = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check, null, probe);

        await Assert.That(probe.StartTypes[0]).IsSameReferenceAs(derived);
    }

    [Test]
    public async Task Seam_IsAskedWithSubstitutionAndTheBlockSetOfTheElementAndOfTheDeclaredType()
    {
        var (schema, @base, derived) = Types(
            XsdDerivationMethod.Extension | XsdDerivationMethod.Substitution,
            XsdDerivationMethod.Restriction
        );
        var check = new ScriptedDerivationCheck().Allow(derived, @base, XsdDerivationMethod.None);

        _ = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check);

        await Assert.That(check.Calls.Count).IsEqualTo(1);
        await Assert.That(check.Calls[0].Derived).IsSameReferenceAs(derived);
        await Assert.That(check.Calls[0].Base).IsSameReferenceAs(@base);
        await Assert.That(check.Calls[0].Method).IsEqualTo(XsdDerivationMethod.Substitution);
        await Assert
            .That(check.Calls[0].Block)
            .IsEqualTo(XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction);
    }

    [Test]
    public async Task BlockedByTheElement_IsReported_AndTheDeclaredTypeAssessesTheContent()
    {
        var (schema, @base, derived) = Types(XsdDerivationMethod.Extension);
        var check = new ScriptedDerivationCheck().Allow(derived, @base, XsdDerivationMethod.Extension);

        var issues = XsiHarness.Run(schema, "\n" + Ext + "><a>x</a></r>", check);

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.3", 2, "'{urn:t}derived'");
        await Assert.That(issues[0].Message).Contains("'r'");
        await Assert.That(issues[0].Message).Contains(ScriptedDerivationCheck.Rule.Id);
    }

    [Test]
    public async Task BlockedByTheDeclaredType_IsReported()
    {
        var (schema, @base, derived) = Types(typeBlock: XsdDerivationMethod.Restriction);
        var check = new ScriptedDerivationCheck().Allow(derived, @base, XsdDerivationMethod.Restriction);

        var issues = XsiHarness.Run(schema, Ext + "><a>x</a></r>", check);

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.3", 1);
    }

    [Test]
    public async Task UnrelatedType_IsReported()
    {
        var (schema, _, _) = Types();

        var issues = XsiHarness.Run(schema, Ext + "><a>x</a></r>", new ScriptedDerivationCheck());

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.3", 1, "is not validly derived");
    }

    [Test]
    public async Task TheDeclaredType_NeedsNoDerivationCheck()
    {
        var (schema, _, _) = Types();
        var check = new ScriptedDerivationCheck();

        var issues = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:base\"><a>x</a></r>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(check.Calls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UnknownType_IsReported_AndTheDeclaredTypeAssessesTheContent()
    {
        var (schema, _, _) = Types();

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + " xsi:type=\"t:nope\"><a>x</a></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.2", 1, "'{urn:t}nope'");
    }

    [Test]
    public async Task UnboundPrefix_IsReported()
    {
        var (schema, _, _) = Types();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns:xsi=\"" + XsiFixture.Xsi + "\" xsi:type=\"q:derived\"><a>x</a></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.1", 1, "'q:derived'");
    }

    [Test]
    [Arguments("1x")]
    [Arguments("a:b:c")]
    [Arguments("")]
    public async Task ValueThatIsNoQName_IsReported(string value)
    {
        var (schema, _, _) = Types();

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + " xsi:type=\"" + value + "\"><a>x</a></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.1", 1);
    }

    [Test]
    public async Task XmlPrefix_IsBound_SoTheTypeIsUnknownRatherThanTheValueInvalid()
    {
        var (schema, _, _) = Types();

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + " xsi:type=\"xml:foo\"><a>x</a></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(
            issues,
            "cvc-elt",
            "cvc-elt.4.2",
            1,
            "'{http://www.w3.org/XML/1998/namespace}foo'"
        );
    }

    [Test]
    public async Task Value_IsCollapsedBeforeItIsResolved()
    {
        var (schema, @base, derived) = Types();
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + " xsi:type=\"&#x20; t:derived&#xA;\"><b>x</b></r>",
            check
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AbstractType_ChosenByXsiType_IsReported()
    {
        var (schema, @base, derived) = Types(abstractDerived: true);
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check);

        await XsiHarness.AssertIssue(issues, "cvc-type", "cvc-type.2", 1, "'{urn:t}derived'");
    }

    [Test]
    public async Task AbstractDeclaredType_WithAConcreteXsiType_IsValid()
    {
        var (schema, @base, derived) = Types(abstractBase: true);
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AbstractDeclaredType_WithoutXsiType_IsReported()
    {
        var (schema, _, _) = Types(abstractBase: true);

        var issues = XsiHarness.Run(schema, "<r><a>x</a></r>");

        await XsiHarness.AssertIssue(issues, "cvc-type", "cvc-type.2", 1, "'{urn:t}base'");
    }

    [Test]
    public async Task SimpleType_ChosenByXsiType_AssessesTheValue()
    {
        var schema = new XsiFixture();
        var @base = schema.Simple("string");
        var derived = schema.Simple("short");
        _ = schema.Global("r", @base);
        var check = new ScriptedDerivationCheck().Allow(derived, @base, XsdDerivationMethod.Restriction);

        var invalid = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:short\">abcd</r>", check);
        var valid = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:short\">abc</r>", check);

        await XsiHarness.AssertIssue(invalid, "cvc-maxLength-valid", null, 1);
        await Assert.That(valid.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ComplexType_ChosenForADeclaredSimpleType_AssessesTheChildren()
    {
        var schema = new XsiFixture();
        var @base = schema.Simple("string");
        var derived = schema.Complex("derived", SchemaFixture.Seq(XsiFixture.Child("b")));
        _ = schema.Global("r", @base);
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(schema, Ext + "><b>x</b></r>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SimpleType_ChosenForADeclaredComplexType_AssessesTheValue()
    {
        var schema = new XsiFixture();
        var @base = schema.Complex("base", SchemaFixture.Seq(XsiFixture.Child("a")));
        var derived = schema.Simple("short");
        _ = schema.Global("r", @base);
        var check = new ScriptedDerivationCheck().Allow(derived, @base, XsdDerivationMethod.Restriction);

        var issues = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:short\">abc</r>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultDerivationCheck_FollowsTheDerivationOfTheBuiltInTypes()
    {
        var schema = new XsiFixture(true);
        _ = schema.Global("r", BuiltIn("string"));
        var assessor = new DescriptorValueAssessor(XsdVersion.V11);
        const string xs = " xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"";

        var valid = XsiHarness.Run(
            schema,
            "<r xmlns:xsi=\"" + XsiFixture.Xsi + "\"" + xs + " xsi:type=\"xs:token\">x</r>",
            null,
            assessor
        );
        var unrelated = XsiHarness.Run(
            schema,
            "<r xmlns:xsi=\"" + XsiFixture.Xsi + "\"" + xs + " xsi:type=\"xs:int\">7</r>",
            null,
            assessor
        );

        await Assert.That(valid.Count).IsEqualTo(0);
        await XsiHarness.AssertIssue(unrelated, "cvc-elt", "cvc-elt.4.3", 1, "'{http://www.w3.org/2001/XMLSchema}int'");
    }

    [Test]
    public async Task XsiTypeAndXsiNil_AreNeverReportedAsUndeclaredAttributes()
    {
        var schema = new XsiFixture();
        _ = schema.Global("r", schema.Complex("empty"));

        var issues = XsiHarness.Run(schema, "<r xmlns:x=\"" + XsiFixture.Xsi + "\" x:type=\"t\" x:nil=\"true\"/>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-elt.4.2");
        await Assert.That(issues[1].Constraint).IsEqualTo("cvc-elt.3.1");
    }

    private static IXsdComponent BuiltIn(string local)
    {
        foreach (var type in XsdBuiltInTypes.For(XsdVersion.V11))
        {
            if (string.Equals(type.Name.LocalName, local, StringComparison.Ordinal))
            {
                return type;
            }
        }

        throw new InvalidOperationException(local);
    }

    // r holds a strict wildcard; "int" is a simple type that the instance may name, the element itself has no declaration.
    private static XsiFixture StrictWildcard()
    {
        var schema = new XsiFixture();
        _ = schema.Simple("int");
        _ = schema.Global(
            "r",
            schema.Complex(
                "rt",
                SchemaFixture.Seq(
                    SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Strict), 0, long.MaxValue)
                )
            )
        );
        return schema;
    }

    [Test]
    public async Task StrictWildcard_UndeclaredElementWithValidXsiType_IsAssessedAgainstThatType()
    {
        var ok = XsiHarness.Run(StrictWildcard(), "<r " + XsiFixture.Prefixes + "><b xsi:type=\"t:int\">12</b></r>");
        var bad = XsiHarness.Run(StrictWildcard(), "<r " + XsiFixture.Prefixes + "><b xsi:type=\"t:int\">ab</b></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await Assert.That(bad.Count).IsEqualTo(1);
        await Assert.That(bad[0].Constraint).IsNotEqualTo("cvc-wildcard.2");
    }

    [Test]
    public async Task StrictWildcard_UndeclaredElementWithoutResolvableXsiType_IsAnError()
    {
        var none = XsiHarness.Run(StrictWildcard(), "<r " + XsiFixture.Prefixes + "><b><c/></b></r>");
        var unknown = XsiHarness.Run(
            StrictWildcard(),
            "<r " + XsiFixture.Prefixes + "><b xsi:type=\"t:nope\"><c/></b></r>"
        );

        await Assert.That(none.Count).IsEqualTo(1);
        await Assert.That(none[0].Constraint).IsEqualTo("cvc-wildcard.2");
        await Assert.That(unknown.Any(i => i.Constraint == "cvc-wildcard.2")).IsTrue();
    }
}
