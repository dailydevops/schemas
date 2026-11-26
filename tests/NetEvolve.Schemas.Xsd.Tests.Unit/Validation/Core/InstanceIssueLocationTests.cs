namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>Issues about end tags, character data, and attributes are located exactly (issue #606).</summary>
public class InstanceIssueLocationTests
{
    // The documents are single line, so the one-based column of a marker is its index plus one.
    private static async Task AssertColumn(IReadOnlyList<ValidationIssue> issues, string xml, string marker)
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location.Line).IsEqualTo(1);
        await Assert.That(issues[0].Location.Column).IsEqualTo(xml.IndexOf(marker, StringComparison.Ordinal) + 1);
    }

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

    [Test]
    public async Task IncompleteContent_IsLocatedAtTheEndTag()
    {
        const string xml = "<r><a>x</a></r>";

        await AssertColumn(ValidationHarness.Run(SequenceOfAAndB(), xml), xml, "</r>");
    }

    [Test]
    public async Task IncompleteContent_OfAnEmptyElementTag_IsLocatedAtTheStartTag()
    {
        const string xml = "<r/>";

        await AssertColumn(ValidationHarness.Run(SequenceOfAAndB(), xml), xml, "<r/>");
    }

    [Test]
    public async Task CharacterData_InElementOnlyContent_IsLocatedAtTheFirstCharacterOfTheTextNode()
    {
        const string xml = "<r><a>x</a> oops<b/></r>";

        await AssertColumn(ValidationHarness.Run(SequenceOfAAndB(), xml), xml, " oops");
    }

    [Test]
    public async Task CharacterData_InEmptyContent_IsLocatedAtTheText()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty());
        const string xml = "<r  >text</r>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "text");
    }

    [Test]
    public async Task FixedElementValue_WithAnotherValue_IsLocatedAtTheText()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            SchemaFixture.Simple("string"),
            new ElementValueConstraint(ElementValueConstraintKind.Fixed, "one")
        );
        const string xml = "<r >two</r>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "two");
    }

    [Test]
    public async Task FixedAttributeValue_WithAnotherValue_IsLocatedAtTheAttributeName()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.Empty(
                schema.Use("a"),
                schema.Use("b", constraint: new AttributeValueConstraint(AttributeValueConstraintKind.Fixed, "one"))
            )
        );
        const string xml = "<r a=\"1\"  b=\"two\"/>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "b=");
    }

    [Test]
    public async Task UndeclaredAttribute_IsLocatedAtTheAttributeName()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty());
        const string xml = "<r  x=\"1\"/>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "x=");
    }

    [Test]
    public async Task AttributeOfASimpleType_IsLocatedAtTheAttributeName()
    {
        var schema = new SchemaFixture();
        schema.Global("r", SchemaFixture.Simple("string"));
        const string xml = "<r  x=\"1\">v</r>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "x=");
    }

    [Test]
    public async Task MissingRequiredAttribute_StaysAtTheStartTag()
    {
        var schema = new SchemaFixture();
        schema.Global("r", schema.Empty(schema.Use("a", kind: AttributeUseKind.Required)));
        const string xml = "<r/>";

        await AssertColumn(ValidationHarness.Run(schema, xml), xml, "<r/>");
    }
}
