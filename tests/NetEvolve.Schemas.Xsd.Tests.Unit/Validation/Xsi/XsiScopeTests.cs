namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

/// <summary>The in-scope namespaces that resolve the prefix of an <c>xsi:type</c> value, and elements without a declaration.</summary>
public class XsiScopeTests
{
    private const string Xsi = " xmlns:xsi=\"" + XsiFixture.Xsi + "\"";

    // Root r (in urn:t) holds any number of c (in no namespace) of the empty type base. derived and plain are empty types too,
    // derived in urn:t and plain in no namespace; the fake derivation check accepts both for base.
    private static (XsiFixture Schema, ScriptedDerivationCheck Check) Schema()
    {
        var schema = new XsiFixture();
        var @base = schema.Complex("base");
        var derived = schema.Complex("derived");
        var plain = schema.Complex("plain", ns: string.Empty);
        var root = schema.Complex("root", SchemaFixture.Seq(SchemaFixture.Part("c", @base, 0, long.MaxValue)));
        _ = schema.Global("r", root, ns: XsiFixture.Ns);
        var check = new ScriptedDerivationCheck()
            .Allow(derived, @base)
            .Allow(plain, @base)
            .Allow(derived, root)
            .Allow(plain, root);
        return (schema, check);
    }

    [Test]
    public async Task Prefix_DeclaredOnAnAncestor_IsInScopeForTheDescendants()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns=\"urn:t\"" + Xsi + " xmlns:t=\"urn:t\"><c xmlns=\"\" xsi:type=\"t:derived\"/></r>",
            check
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Prefix_DeclaredOnASibling_IsOutOfScopeAfterThatSiblingEnds()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns=\"urn:t\""
                + Xsi
                + ">\n<c xmlns=\"\" xmlns:t=\"urn:t\"/>\n<c xmlns=\"\" xsi:type=\"t:derived\"/></r>",
            check
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.1", 3, "'t:derived'");
    }

    [Test]
    public async Task Prefix_DeclaredAgainOnAChild_ShadowsTheDeclarationOfTheParent()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns=\"urn:t\""
                + Xsi
                + " xmlns:t=\"urn:other\"><c xmlns=\"\" xmlns:t=\"urn:t\" xsi:type=\"t:derived\"/></r>",
            check
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Prefix_BoundToAnotherNamespace_NamesAnUnknownType()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns=\"urn:t\"" + Xsi + " xmlns:t=\"urn:other\" xsi:type=\"t:derived\"/>",
            check
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.2", 1, "'{urn:other}derived'");
    }

    [Test]
    public async Task UnprefixedValue_UsesTheDefaultNamespace()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(schema, "<r xmlns=\"urn:t\"" + Xsi + " xsi:type=\"derived\"/>", check);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UnprefixedValue_WithoutADefaultNamespace_NamesATypeInNoNamespace()
    {
        var (schema, check) = Schema();

        var issues = XsiHarness.Run(
            schema,
            "<r xmlns=\"urn:t\"" + Xsi + "><c xmlns=\"\" xsi:type=\"plain\"/></r>",
            check
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Declaration_InASkippedSubtree_DoesNotLeakIntoTheElementsThatFollow()
    {
        var schema = new XsiFixture();
        var @base = schema.Complex("base");
        var derived = schema.Complex("derived");
        var root = schema.Complex(
            "root",
            SchemaFixture.Seq(
                SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Skip)),
                SchemaFixture.Part("c", @base)
            )
        );
        _ = schema.Global("r", root);
        var check = new ScriptedDerivationCheck().Allow(derived, @base);

        var issues = XsiHarness.Run(
            schema,
            "<r" + Xsi + ">\n<z xmlns:t=\"urn:t\"><y xmlns:t=\"urn:t\"/></z>\n<c xsi:type=\"t:derived\"/></r>",
            check
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.1", 3);
    }

    [Test]
    public async Task UndeclaredRoot_WithAUsableXsiType_IsAssessedAgainstThatType()
    {
        var schema = new XsiFixture();
        _ = schema.Complex("derived", SchemaFixture.Seq(XsiFixture.Child("b")));
        var check = new ScriptedDerivationCheck();

        var valid = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:derived\"><b>x</b></r>", check);
        var invalid = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + " xsi:type=\"t:derived\">\n<a>x</a></r>",
            check
        );

        await Assert.That(valid.Count).IsEqualTo(0);
        await XsiHarness.AssertIssue(invalid, "cvc-complex-type", "cvc-complex-type.2.4", 2, "'a'");
        await Assert.That(check.Calls.Count).IsEqualTo(0);
    }

    [Test]
    public async Task UndeclaredRoot_WithAnUnknownXsiType_IsReportedForTheTypeAndForTheMissingDeclaration()
    {
        var schema = new XsiFixture();

        var issues = XsiHarness.Run(schema, "<r " + XsiFixture.Prefixes + " xsi:type=\"t:nope\"/>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-elt.4.2");
        await Assert.That(issues[1].Constraint).IsEqualTo("cvc-assess-elt.1");
        await Assert.That(issues[1].Code).IsEqualTo(ValidationHarness.Code("cvc-assess-elt"));
    }

    [Test]
    public async Task LaxWildcardElement_WithoutADeclaration_IsAssessedAgainstItsXsiType()
    {
        var schema = new XsiFixture();
        _ = schema.Complex("derived", SchemaFixture.Seq(XsiFixture.Child("b")));
        _ = schema.Global(
            "r",
            schema.Complex(
                "root",
                SchemaFixture.Seq(SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax)))
            )
        );

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + ">\n<z xsi:type=\"t:derived\"><a>x</a></z></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 2, "'a'");
    }

    [Test]
    public async Task LaxWildcardElement_WithAnUnknownXsiType_IsReportedOnlyForTheType()
    {
        var schema = new XsiFixture();
        _ = schema.Global(
            "r",
            schema.Complex(
                "root",
                SchemaFixture.Seq(SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Lax)))
            )
        );

        var issues = XsiHarness.Run(
            schema,
            "<r " + XsiFixture.Prefixes + "><z xsi:type=\"t:nope\"/></r>",
            new ScriptedDerivationCheck()
        );

        await XsiHarness.AssertIssue(issues, "cvc-elt", "cvc-elt.4.2", 1);
    }
}
