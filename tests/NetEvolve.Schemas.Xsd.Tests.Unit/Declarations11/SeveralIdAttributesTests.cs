namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

/// <summary>
/// The id table of a validation run takes the values of every ID attribute of an element. The table is the listener of the
/// ID checks (#303); these tests pin that it does not limit an element to one ID attribute, which XSD 1.1 allows.
/// </summary>
public class SeveralIdAttributesTests
{
    private static (SchemaFixture Schema, IReadOnlyList<ValidationIssue> BuildIssues) TwoIds()
    {
        var schema = new SchemaFixture();
        var e = schema.Empty(schema.Use("a", "ID"), schema.Use("b", "ID"));
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Group(ModelGroupCompositor.Choice, 0, long.MaxValue, SchemaFixture.Part("e", e))
            )
        );

        IdAttributeUses.CheckType(e.AttributeUses, XsdVersion.V11, schema.Reporter);
        return (schema, schema.Collector.ToResult().Issues);
    }

    private static IReadOnlyList<ValidationIssue> Run(SchemaFixture schema, string xml) =>
        ValidationHarness.Run(schema, xml, null, IdFixture.Listener(new ScriptedAssessor()));

    [Test]
    public async Task Compile_Version11_TwoIdAttributesOnOneElement_ReportNoSchemaError()
    {
        var (_, buildIssues) = TwoIds();

        await Assert.That(buildIssues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Run_TwoIdAttributesWithDistinctValues_ReportNothing()
    {
        var (schema, _) = TwoIds();

        var issues = Run(schema, "<r><e a=\"p\" b=\"q\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Run_TheSecondAttributeRepeatingTheFirst_IsADuplicate()
    {
        var (schema, _) = TwoIds();

        var issues = Run(schema, "<r>\n<e a=\"p\" b=\"p\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-id"));
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-id.2");
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Run_TheValueOfTheSecondAttribute_IsInTheTable()
    {
        var (schema, _) = TwoIds();

        var issues = Run(schema, "<r>\n<e a=\"p\" b=\"q\"/>\n<e a=\"q\" b=\"r\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-id.2");
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Run_TheValueOfTheFirstAttribute_IsInTheTable()
    {
        var (schema, _) = TwoIds();

        var issues = Run(schema, "<r>\n<e a=\"p\" b=\"q\"/>\n<e a=\"r\" b=\"p\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-id.2");
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }
}
