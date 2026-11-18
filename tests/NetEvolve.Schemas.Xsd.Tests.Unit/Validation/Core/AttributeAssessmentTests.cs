namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

public class AttributeAssessmentTests
{
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    private static async Task AssertIssue(
        IReadOnlyList<ValidationIssue> issues,
        string rule,
        string clause,
        int line,
        string? messagePart = null
    )
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code(rule));
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsGreaterThan(0);
        if (messagePart is not null)
        {
            await Assert.That(issues[0].Message).Contains(messagePart);
        }
    }

    private static SchemaFixture RootWith(Func<SchemaFixture, ComplexTypeDefinition> type)
    {
        var schema = new SchemaFixture();
        schema.Global("r", type(schema));
        return schema;
    }

    [Test]
    public async Task DeclaredAttributes_AreValid()
    {
        var schema = RootWith(s => s.Empty(s.Use("a"), s.Use("b", kind: AttributeUseKind.Required)));

        var issues = ValidationHarness.Run(schema, "<r a=\"1\" b=\"2\"/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OptionalAttribute_MayBeAbsent()
    {
        var schema = RootWith(s => s.Empty(s.Use("a")));

        var issues = ValidationHarness.Run(schema, "<r/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RequiredAttribute_Missing_IsReportedAtTheStartTag()
    {
        var schema = RootWith(s => s.Empty(s.Use("a", kind: AttributeUseKind.Required)));

        var issues = ValidationHarness.Run(schema, "\n\n<r/>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.4", 3, "'a'");
    }

    [Test]
    public async Task UndeclaredAttribute_WithoutWildcard_IsAnError()
    {
        var schema = RootWith(s => s.Empty(s.Use("a")));

        var issues = ValidationHarness.Run(schema, "<r\n a=\"1\" z=\"2\"/>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.3.2.1", 1, "'z'");
    }

    [Test]
    public async Task ProhibitedAttribute_OfARestriction_IsUndeclared()
    {
        var schema = new SchemaFixture();
        var baseType = schema.Empty(schema.Use("a"), schema.Use("b"));
        var restricted = ComplexTypeDefinition.FromComplexContent(
            SchemaFixture.Header("d"),
            XsdDerivationMethod.Restriction,
            baseType,
            false,
            null,
            [schema.Use("a", kind: AttributeUseKind.Prohibited)],
            null,
            schema.Reporter
        )!;
        schema.Global("r", restricted);

        var issues = ValidationHarness.Run(schema, "<r a=\"1\" b=\"2\"/>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.3.2.1", 1, "'a'");
    }

    [Test]
    [Arguments("<r x:type=\"t\" x:nil=\"true\"/>")]
    [Arguments("<r x:schemaLocation=\"urn:a b.xsd\" x:noNamespaceSchemaLocation=\"c.xsd\"/>")]
    public async Task XsiAttributes_AreAlwaysAllowed(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var schema = RootWith(s => s.Empty());
        var document = xml.Replace("<r ", "<r xmlns:x=\"" + XsiNamespace + "\" ", StringComparison.Ordinal);

        var issues = ValidationHarness.Run(schema, document);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OtherXsiAttribute_IsUndeclared()
    {
        var schema = RootWith(s => s.Empty());

        var issues = ValidationHarness.Run(schema, "<r xmlns:x=\"" + XsiNamespace + "\" x:other=\"1\"/>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.3.2.1", 1, "}other'");
    }

    [Test]
    public async Task SimpleTypedElement_WithAnAttribute_IsAnError()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"));

        var ok = ValidationHarness.Run(schema, "<r xmlns:q=\"urn:q\"/>");
        var bad = ValidationHarness.Run(schema, "<r a=\"1\"/>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-type", "cvc-type.3.1.1", 1, "'a'");
    }

    [Test]
    [Arguments("##any", true)]
    [Arguments("##other", false)]
    [Arguments("##local", true)]
    [Arguments("urn:other", false)]
    public async Task AttributeWildcard_AcceptsTheAttributesItsNamespaceConstraintAllows(
        string constraint,
        bool allowed
    )
    {
        var schema = RootWith(s => s.WithAttributes(SchemaFixture.Wild(constraint, XsdProcessContents.Skip)));

        var issues = ValidationHarness.Run(schema, "<r z=\"1\"/>");

        await Assert.That(issues.Count == 0).IsEqualTo(allowed);
        if (!allowed)
        {
            await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-wildcard-namespace"));
            await Assert.That(issues[0].Constraint).IsEqualTo("cvc-wildcard-namespace");
        }
    }

    [Test]
    public async Task AttributeWildcard_Strict_WithoutDeclaration_IsAnError()
    {
        var schema = RootWith(s => s.WithAttributes(SchemaFixture.Wild("##any", XsdProcessContents.Strict)));

        var issues = ValidationHarness.Run(schema, "<r z=\"1\"/>");

        await AssertIssue(issues, "cvc-wildcard", "cvc-wildcard.2", 1, "'z'");
    }

    [Test]
    public async Task AttributeWildcard_Strict_WithDeclaration_AssessesTheValue()
    {
        var schema = RootWith(s => s.WithAttributes(SchemaFixture.Wild("##any", XsdProcessContents.Strict)));
        schema.GlobalAttribute("n", SchemaFixture.Simple("int"));

        var ok = ValidationHarness.Run(schema, "<r n=\"12\"/>");
        var bad = ValidationHarness.Run(schema, "<r n=\"x\"/>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-datatype-valid", "cvc-datatype-valid", 1, "'n'");
    }

    [Test]
    public async Task AttributeWildcard_Lax_AssessesOnlyWhenADeclarationExists()
    {
        var schema = RootWith(s => s.WithAttributes(SchemaFixture.Wild("##any", XsdProcessContents.Lax)));
        schema.GlobalAttribute("n", SchemaFixture.Simple("int"));

        var undeclared = ValidationHarness.Run(schema, "<r z=\"anything\"/>");
        var declaredInvalid = ValidationHarness.Run(schema, "<r n=\"x\"/>");

        await Assert.That(undeclared.Count).IsEqualTo(0);
        await Assert.That(declaredInvalid.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AttributeWildcard_Skip_NeverAssesses_EvenWhenADeclarationExists()
    {
        var schema = RootWith(s => s.WithAttributes(SchemaFixture.Wild("##any", XsdProcessContents.Skip)));
        schema.GlobalAttribute("n", SchemaFixture.Simple("int"));

        var issues = ValidationHarness.Run(schema, "<r n=\"x\"/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AttributeValue_InvalidAgainstItsType_IsReportedByTheDatatypeRule()
    {
        var schema = RootWith(s => s.Empty(s.Use("n", "int")));

        var issues = ValidationHarness.Run(schema, "<r n=\"x\"/>");

        await AssertIssue(issues, "cvc-datatype-valid", "cvc-datatype-valid", 1, "'n'");
    }

    [Test]
    public async Task AttributeValue_FailingAFacet_IsReportedByTheRuleOfTheFacet()
    {
        var schema = RootWith(s => s.Empty(s.Use("n", "short")));

        var issues = ValidationHarness.Run(schema, "<r n=\"12345\"/>");

        await AssertIssue(issues, "cvc-maxLength-valid", "cvc-maxLength-valid", 1, "'n'");
    }

    [Test]
    public async Task FixedAttribute_WithAnotherValue_IsAnError()
    {
        var schema = RootWith(s =>
            s.Empty(s.Use("a", constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "one")))
        );

        var ok = ValidationHarness.Run(schema, "<r a=\"one\"/>");
        var bad = ValidationHarness.Run(schema, "<r a=\"two\"/>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-au", "cvc-au", 1, "'a'");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task FixedAttribute_ReachedThroughAnAttributeWildcard_WithAnotherValue_IsAnError(bool strict)
    {
        var schema = RootWith(s =>
            s.WithAttributes(SchemaFixture.Wild("##any", strict ? XsdProcessContents.Strict : XsdProcessContents.Lax))
        );
        _ = schema.GlobalAttribute(
            "f",
            SchemaFixture.Simple("string"),
            constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "one")
        );

        var ok = ValidationHarness.Run(schema, "<r f=\"one\"/>");
        var bad = ValidationHarness.Run(schema, "<r f=\"two\"/>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-au", "cvc-au", 1, "'f'");
    }

    [Test]
    public async Task AbsentAttribute_WithADefault_IsAppliedAndReportedToListeners()
    {
        var schema = RootWith(s =>
            s.Empty(
                s.Use("a", constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "dflt")),
                s.Use("b", constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "fix"))
            )
        );
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(schema, "<r/>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(
                string.Join(
                    " | ",
                    "begin",
                    "start r#1@1 line 1",
                    "attr a=dflt (default)",
                    "attr b=fix (default)",
                    "end #1",
                    "endrun"
                )
            );
    }

    [Test]
    public async Task PresentAttribute_WithADefault_KeepsItsOwnValue()
    {
        var schema = RootWith(s =>
            s.Empty(s.Use("a", constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "dflt")))
        );
        var listener = new RecordingListener();

        _ = ValidationHarness.Run(schema, "<r a=\"mine\"/>", null, listener);

        await Assert.That(listener.Events).Contains("attr a=mine");
        await Assert.That(listener.Events).DoesNotContain("attr a=dflt (default)");
    }
}
