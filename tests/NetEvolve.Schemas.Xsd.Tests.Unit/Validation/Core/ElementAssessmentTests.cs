namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Elements;

public class ElementAssessmentTests
{
    private static SchemaFixture SequenceOfAAndB()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(
                    SchemaFixture.Part("a", SchemaFixture.Simple("string")),
                    SchemaFixture.Part("b", SchemaFixture.Simple("string"))
                )
            )
        );
        return schema;
    }

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
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsGreaterThan(0);
        await Assert.That(issues[0].Location.Source).IsEqualTo(ValidationHarness.Source);
        if (messagePart is not null)
        {
            await Assert.That(issues[0].Message).Contains(messagePart);
        }
    }

    [Test]
    public async Task Sequence_ValidInstance_HasNoIssues()
    {
        var issues = ValidationHarness.Run(SequenceOfAAndB(), "<r><a>x</a><b/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Sequence_UnexpectedChild_IsReportedAtTheChildWithTheExpectedNames()
    {
        var issues = ValidationHarness.Run(SequenceOfAAndB(), "<r>\n<b/>\n<a/></r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 2, "'b'");
        await Assert.That(issues[0].Message).Contains("'a'");
    }

    [Test]
    public async Task Sequence_MissingChild_IsReportedAtTheEndTagOfTheParent()
    {
        var issues = ValidationHarness.Run(SequenceOfAAndB(), "\n<r>\n<a/>\n</r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 4, "'b'");
    }

    [Test]
    public async Task Sequence_AfterAnUnexpectedChild_ReportsTheContentOnlyOnce()
    {
        var issues = ValidationHarness.Run(SequenceOfAAndB(), "<r><z/><a/><a/><a/></r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.4", 1, "'z'");
    }

    [Test]
    public async Task UnexpectedChild_ItsSubtreeIsNotAssessed()
    {
        var issues = ValidationHarness.Run(SequenceOfAAndB(), "<r><z><y q=\"1\">text</y></z></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("<r><a/></r>", 0)]
    [Arguments("<r><b/></r>", 0)]
    [Arguments("<r><c/></r>", 1)]
    [Arguments("<r/>", 1)]
    [Arguments("<r><a/><b/></r>", 1)]
    public async Task Choice_AcceptsExactlyOneAlternative(string xml, int expectedIssues)
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Choice(
                    SchemaFixture.Part("a", SchemaFixture.Simple("string")),
                    SchemaFixture.Part("b", SchemaFixture.Simple("string"))
                )
            )
        );

        var issues = ValidationHarness.Run(schema, xml);

        await Assert.That(issues.Count).IsEqualTo(expectedIssues);
    }

    [Test]
    [Arguments("<r><a/><b/></r>", 0, XsdVersion.V10)]
    [Arguments("<r><b/><a/></r>", 0, XsdVersion.V10)]
    [Arguments("<r><b/></r>", 0, XsdVersion.V10)]
    [Arguments("<r><a/></r>", 1, XsdVersion.V10)]
    [Arguments("<r><b/><b/></r>", 1, XsdVersion.V10)]
    [Arguments("<r><b/><c/></r>", 1, XsdVersion.V10)]
    [Arguments("<r><a/><b/></r>", 0, XsdVersion.V11)]
    [Arguments("<r><b/><a/></r>", 0, XsdVersion.V11)]
    [Arguments("<r><b/></r>", 0, XsdVersion.V11)]
    [Arguments("<r><a/></r>", 1, XsdVersion.V11)]
    [Arguments("<r><b/><b/></r>", 1, XsdVersion.V11)]
    [Arguments("<r><b/><c/></r>", 1, XsdVersion.V11)]
    public async Task AllGroup_AcceptsAnyOrderAndEachMemberOnce(string xml, int expectedIssues, XsdVersion version)
    {
        var schema = new SchemaFixture { Version = version };
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.All(
                    SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, 1),
                    SchemaFixture.Part("b", SchemaFixture.Simple("string"))
                )
            )
        );

        var issues = ValidationHarness.Run(schema, xml);

        await Assert.That(issues.Count).IsEqualTo(expectedIssues);
    }

    [Test]
    [Arguments(0, false)]
    [Arguments(1, false)]
    [Arguments(2, true)]
    [Arguments(3, true)]
    [Arguments(4, false)]
    public async Task CountedOccurrence_FollowsMinAndMaxOccurs(int count, bool valid)
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"), 2, 3)))
        );

        var issues = ValidationHarness.Run(schema, "<r>" + Repeat("<a/>", count) + "</r>");

        await Assert.That(issues.Count == 0).IsEqualTo(valid);
    }

    [Test]
    public async Task CountedNesting_ExceedingTheRunCeiling_IsReportedAsAnEngineLimitAndNotAsInvalidContent()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Group(
                    ModelGroupCompositor.Sequence,
                    12,
                    100,
                    SchemaFixture.Group(
                        ModelGroupCompositor.Sequence,
                        12,
                        100,
                        SchemaFixture.Part("a", SchemaFixture.Simple("string"), 12, 100)
                    )
                )
            )
        );

        var issues = ValidationHarness.Run(schema, "<r>\n" + Repeat("<a/>", 5000) + "</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("engine-content-model-transitions"));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("1024");
    }

    [Test]
    public async Task ElementOnlyContent_AllowsWhitespace()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, 1)))
        );

        var issues = ValidationHarness.Run(schema, "<r>\n  <a/>\t\r\n</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ElementOnlyContent_RejectsOtherText()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, 1)))
        );

        var issues = ValidationHarness.Run(schema, "<r>\n<a/>oops\n</r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.3", 2);
    }

    [Test]
    public async Task MixedContent_AllowsTextBetweenChildren()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.Mixed(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, long.MaxValue)))
        );

        var issues = ValidationHarness.Run(schema, "<r>one<a/>two<a/>three</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EmptyContent_AcceptsAnEmptyElement()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty());

        var issues = ValidationHarness.Run(schema, "<r/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EmptyContent_RejectsChildren()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty());

        var issues = ValidationHarness.Run(schema, "\n<r><a/></r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.1", 2, "'a'");
    }

    [Test]
    public async Task EmptyContent_RejectsAnyCharacter_EvenWhitespace()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty());

        var issues = ValidationHarness.Run(schema, "<r> </r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.1", 1);
    }

    [Test]
    public async Task SimpleContent_RejectsChildElements()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.SimpleContent("string"));

        var issues = ValidationHarness.Run(schema, "<r>x<a/></r>");

        await AssertIssue(issues, "cvc-complex-type", "cvc-complex-type.2.2", 1, "'a'");
    }

    [Test]
    public async Task SimpleType_RejectsChildElements()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"));

        var issues = ValidationHarness.Run(schema, "<r><a/></r>");

        await AssertIssue(issues, "cvc-type", "cvc-type.3.1.2", 1, "'a'");
    }

    [Test]
    public async Task UndeclaredRoot_IsAnAssessmentError()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"));

        var issues = ValidationHarness.Run(schema, "\n<other/>");

        await AssertIssue(issues, "cvc-assess-elt", "cvc-assess-elt.1", 2, "'other'");
    }

    [Test]
    public async Task UndeclaredRoot_WithoutAnySchema_IsAnAssessmentError()
    {
        var issues = ValidationHarness.Run(new SchemaFixture(), "<r/>");

        await AssertIssue(issues, "cvc-assess-elt", "cvc-assess-elt.1", 1);
    }

    [Test]
    public async Task UndeclaredRoot_ItsDeclaredChildrenAreAssessedLax()
    {
        var schema = new SchemaFixture();
        schema.Global("known", schema.Empty());

        var issues = ValidationHarness.Run(schema, "<other><known>text</known><unknown>text</unknown></other>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-assess-elt"));
        await Assert.That(issues[1].Code).IsEqualTo(ValidationHarness.Code("cvc-complex-type"));
    }

    [Test]
    public async Task AbstractRootDeclaration_IsAnError()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"), isAbstract: true);

        var issues = ValidationHarness.Run(schema, "<r/>");

        await AssertIssue(issues, "cvc-elt", "cvc-elt.2", 1, "'r'");
    }

    [Test]
    public async Task AbstractType_IsAnError()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            ComplexTypeDefinition.Implicit(
                SchemaFixture.Header("t", isAbstract: true),
                false,
                null,
                [],
                null,
                schema.Reporter
            )
        );

        var issues = ValidationHarness.Run(schema, "<r/>");

        await AssertIssue(issues, "cvc-type", "cvc-type.2", 1, "'t'");
    }

    [Test]
    public async Task ElementWithoutType_IsAssessedAsAnyType()
    {
        var schema = new SchemaFixture();
        _ = schema.Global("r", null);

        var issues = ValidationHarness.Run(schema, "<r a=\"1\">text<b><c/></b>more</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DeclaredAnyType_AssessesItsContentLax_AndAcceptsAnyAttribute()
    {
        var schema = new SchemaFixture();
        _ = schema.Global("r", SchemaFixture.AnyType);
        _ = schema.Global("n", SchemaFixture.Simple("int"));

        var ok = ValidationHarness.Run(schema, "<r a=\"1\">text<b><c/></b><n>12</n>more</r>");
        var bad = ValidationHarness.Run(schema, "<r>\n<b/>\n<n>x</n></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await AssertIssue(bad, "cvc-datatype-valid", "cvc-datatype-valid", 3, "'n'");
    }

    [Test]
    public async Task Nesting_BeyondTheInitialFrameCapacity_KeepsEveryLevelAssessed()
    {
        var schema = new SchemaFixture();
        _ = schema.Global("r", SchemaFixture.AnyType);
        _ = schema.Global("n", SchemaFixture.Simple("int"));
        var open = Repeat("<a>\n", 40);
        var close = Repeat("</a>", 40);

        var issues = ValidationHarness.Run(schema, "<r>\n" + open + "<n>x</n>" + close + "<n>1</n></r>");

        await AssertIssue(issues, "cvc-datatype-valid", "cvc-datatype-valid", 42, "'n'");
    }

    [Test]
    public async Task Doctype_IsIgnored()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"))))
        );

        var issues = ValidationHarness.Run(
            schema,
            "<!DOCTYPE r>\n<r><a/></r>",
            XmlReaderOptions.Default.WithDtdProcessing(true)
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Names_AreMatchedByNamespaceAndLocalName_NotByPrefix()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(
                    SchemaFixture.Part(
                        SchemaFixture.Element("a", SchemaFixture.Simple("string"), namespaceName: "urn:x")
                    )
                )
            )
        );

        var ok = ValidationHarness.Run(schema, "<r xmlns:p=\"urn:x\"><p:a/></r>");
        var otherPrefix = ValidationHarness.Run(schema, "<r xmlns:q=\"urn:x\"><q:a/></r>");
        var wrongNamespace = ValidationHarness.Run(schema, "<r><a/></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await Assert.That(otherPrefix.Count).IsEqualTo(0);
        await Assert.That(wrongNamespace.Count).IsEqualTo(1);
    }

    [Test]
    public async Task NestedTypes_EachElementIsAssessedAgainstItsOwnType()
    {
        var schema = new SchemaFixture();
        var inner = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("leaf", SchemaFixture.Simple("string"))));
        schema.Global("r", schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("item", inner, 0, long.MaxValue))));

        var issues = ValidationHarness.Run(
            schema,
            "<r><item><leaf/></item><item><leaf/></item><item><bad/></item><item><leaf/></item></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task ModelGroupDefinition_IsFollowedLikeAnInlineGroup()
    {
        var schema = new SchemaFixture();
        var definition = ModelGroupDefinition.Create(
            new XPathQName(string.Empty, "g"),
            SchemaFixture.Where,
            ModelGroup.Create(
                ModelGroupCompositor.Sequence,
                SchemaFixture.Where,
                [
                    SchemaFixture.Part("a", SchemaFixture.Simple("string")),
                    SchemaFixture.Part("b", SchemaFixture.Simple("string")),
                ]
            )
        );
        schema.Global("r", schema.ElementOnly(SchemaFixture.Part(definition)));

        var ok = ValidationHarness.Run(schema, "<r><a/><b/></r>");
        var bad = ValidationHarness.Run(schema, "<r><b/></r>");

        await Assert.That(ok.Count).IsEqualTo(0);
        await Assert.That(bad.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ContentModelState_IsIndependentPerElement_AlsoWhenRunsAreReused()
    {
        var schema = new SchemaFixture();
        var item = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("x", SchemaFixture.Simple("string"), 2, 2)));
        var other = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("y", SchemaFixture.Simple("string"))));
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(
                    SchemaFixture.Part("item", item, 0, long.MaxValue),
                    SchemaFixture.Part("other", other, 0, long.MaxValue)
                )
            )
        );

        var valid = ValidationHarness.Run(
            schema,
            "<r><item><x/><x/></item><item><x/><x/></item><other><y/></other></r>"
        );
        var oneBad = ValidationHarness.Run(
            schema,
            "<r><item><x/><x/></item><item><x/></item><item><x/><x/></item></r>"
        );

        await Assert.That(valid.Count).IsEqualTo(0);
        await Assert.That(oneBad.Count).IsEqualTo(1);
        await Assert.That(oneBad[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task ContentModelAboveTheCeiling_IsReportedWhenTheModelIsBuilt_AndItsChildrenAreNotAssessed()
    {
        var schema = new SchemaFixture();
        var alternatives = new Particle[65_537];
        for (var i = 0; i < alternatives.Length; i++)
        {
            alternatives[i] = SchemaFixture.Part("e" + i, SchemaFixture.Simple("string"));
        }

        schema.Global("r", schema.ElementOnly(SchemaFixture.Choice(alternatives)));

        var issues = ValidationHarness.Run(schema, "<r><unknown/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(schema.Collector.ToResult().Issues.Count).IsEqualTo(1);
        await Assert
            .That(schema.Collector.ToResult().Issues[0].Code)
            .IsEqualTo(XsdRuleCodes.ContentModelPositionCeilingExceeded);
    }

    private static string Repeat(string text, int count)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < count; i++)
        {
            _ = builder.Append(text);
        }

        return builder.ToString();
    }
}
