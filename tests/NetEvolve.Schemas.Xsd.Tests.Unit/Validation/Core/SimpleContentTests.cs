namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

public class SimpleContentTests
{
    private static ElementValueConstraint Default(string value) => new(ElementValueConstraintKind.Default, value);

    private static ElementValueConstraint Fixed(string value) => new(ElementValueConstraintKind.Fixed, value);

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
        if (messagePart is not null)
        {
            await Assert.That(issues[0].Message).Contains(messagePart);
        }
    }

    [Test]
    [Arguments("<r>42</r>", 0)]
    [Arguments("<r>4x2</r>", 1)]
    [Arguments("<r></r>", 1)]
    [Arguments("<r/>", 1)]
    public async Task SimpleTypedElement_ValueIsAssessedAgainstTheType(string xml, int expectedIssues)
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("int"));

        var issues = ValidationHarness.Run(schema, xml);

        await Assert.That(issues.Count).IsEqualTo(expectedIssues);
    }

    [Test]
    public async Task SimpleContentOfAComplexType_IsAssessedAgainstTheContentType()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.SimpleContent("int", schema.Use("a")));

        var ok = ValidationHarness.Run(schema, "<r a=\"x\">7</r>");
        var bad = ValidationHarness.Run(schema, "<r a=\"x\">seven</r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-datatype-valid", "cvc-datatype-valid", 1, "'r'");
    }

    [Test]
    public async Task Value_FailingAFacet_IsReportedByTheRuleOfTheFacet()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("short"));

        var issues = ValidationHarness.Run(schema, "\n<r>12345</r>");

        await AssertIssue(issues, "cvc-maxLength-valid", "cvc-maxLength-valid", 2, "'r'");
    }

    [Test]
    public async Task Value_FailingAPattern_IsReportedByThePatternRule()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("letters"));

        var issues = ValidationHarness.Run(schema, "<r>ab1</r>");

        await AssertIssue(issues, "cvc-pattern-valid", "cvc-pattern-valid", 1);
    }

    [Test]
    public async Task SplitText_IsCoalescedBeforeTheValueIsAssessed()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("int"));
        var assessor = new ScriptedAssessor();
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(schema, "<r>12&#x33;<![CDATA[4]]>5</r>", assessor, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(assessor.Calls).IsEqualTo(1);
        await Assert.That(listener.Events).Contains("value 12345");
    }

    [Test]
    public async Task Comments_DoNotSplitOrChangeTheValue()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("int"));

        var issues = ValidationHarness.Run(schema, "<r>12<!-- c -->34<?pi x?></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FixedElementValue_WithAnotherValue_IsAnError()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"), Fixed("one"));

        var ok = ValidationHarness.Run(schema, "<r>one</r>");
        var bad = ValidationHarness.Run(schema, "<r>two</r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_EqualInTheValueSpace_IsValid()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("decimal"), Fixed("1"));

        var ok = ValidationHarness.Run(schema, "<r>1.0</r>");
        var bad = ValidationHarness.Run(schema, "<r>2.0</r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_OfAnySimpleType_KeepsItsWhiteSpace()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("anySimpleType"), Fixed("a b"));

        var ok = ValidationHarness.Run(schema, "<r>a b</r>");
        var bad = ValidationHarness.Run(schema, "<r> a b </r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    private static IXsdComponent Builtin(string local)
    {
        foreach (var type in XsdBuiltInTypes.For(XsdVersion.V11))
        {
            if (string.Equals(type.Name.LocalName, local, StringComparison.Ordinal))
            {
                return type;
            }
        }

        throw new InvalidOperationException("No built-in type " + local);
    }

    [Test]
    [Arguments("anySimpleType")]
    [Arguments("anyAtomicType")]
    public async Task FixedElementValue_OfABuiltInUrType_KeepsItsWhiteSpace(string urType)
    {
        var schema = new SchemaFixture();
        schema.Global("r", Builtin(urType), Fixed("a b"));

        var ok = ValidationHarness.Run(schema, "<r>a b</r>");
        var bad = ValidationHarness.Run(schema, "<r> a b </r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_OfAnAnyAtomicTypeDefinition_KeepsItsWhiteSpace()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("anyAtomicType"), Fixed("a b"));

        var bad = ValidationHarness.Run(schema, "<r> a b </r>");

        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_OfABuiltInType_IsComparedInTheValueSpace()
    {
        var schema = new SchemaFixture();
        schema.Global("r", Builtin("decimal"), Fixed("1"));

        var ok = ValidationHarness.Run(schema, "<r>1.0</r>");
        var bad = ValidationHarness.Run(schema, "<r>2.0</r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_WithoutALexicalValue_DiffersFromAnyText()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            SchemaFixture.Simple("string"),
            new ElementValueConstraint(ElementValueConstraintKind.Fixed, null!)
        );

        var bad = ValidationHarness.Run(schema, "<r>x</r>");

        await AssertIssue(bad, "cvc-elt", "cvc-elt.5.2.2.2.2", 1, "'r'");
    }

    [Test]
    public async Task DefaultElementValue_IsAppliedToAnEmptyElementAndReportedToListeners()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("int"), Default("7"));
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(schema, "<r/>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(string.Join(" | ", "begin", "start r#1@1 line 1", "value 7 (default)", "end #1", "endrun"));
    }

    [Test]
    public async Task DefaultElementValue_IsNotAppliedWhenTheElementHasText()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"), Default("dflt"));
        var listener = new RecordingListener();

        _ = ValidationHarness.Run(schema, "<r>mine</r>", null, listener);

        await Assert.That(listener.Events).Contains("value mine");
        await Assert.That(listener.Events).DoesNotContain("value dflt (default)");
    }

    [Test]
    public async Task DefaultElementValue_IsAssessedAgainstTheType()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("int"), Default("seven"));

        var issues = ValidationHarness.Run(schema, "<r/>");

        await AssertIssue(issues, "cvc-datatype-valid", "cvc-datatype-valid", 1, "'r'");
    }

    [Test]
    public async Task FixedElementValue_IsAppliedToAnEmptyElement()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"), Fixed("one"));
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(schema, "<r/>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(listener.Events).Contains("value one (default)");
    }

    [Test]
    public async Task DefaultValue_OfAnElementWithElementOnlyContent_IsNeverApplied()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty(), Default("x"));
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(schema, "<r/>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(listener.Events).DoesNotContain("value x (default)");
    }
}
