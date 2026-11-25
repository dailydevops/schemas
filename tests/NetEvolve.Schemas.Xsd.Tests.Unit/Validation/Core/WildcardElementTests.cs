namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Xsd.Contracts;

public class WildcardElementTests
{
    private static SchemaFixture WildcardRoot(string namespaces, XsdProcessContents process, out SchemaFixture schema)
    {
        schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(SchemaFixture.Part(SchemaFixture.Wild(namespaces, process), 0, long.MaxValue))
            )
        );

        // A declared element whose content is invalid unless it is empty.
        schema.Global("known", schema.Empty());
        return schema;
    }

    [Test]
    public async Task Strict_WithoutDeclaration_IsAnError()
    {
        var issues = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Strict, out _),
            "<r>\n<unknown/></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-wildcard"));
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-wildcard.2");
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Message).Contains("'unknown'");
    }

    [Test]
    public async Task Strict_WithDeclaration_AssessesTheElement()
    {
        var schema = WildcardRoot("##any", XsdProcessContents.Strict, out _);

        var ok = ValidationHarness.Run(schema, "<r><known/></r>");
        var bad = ValidationHarness.Run(schema, "<r><known>text</known></r>");

        await Assert.That(string.Join("//", ok.Select(i => i.Constraint + i.Message))).IsEqualTo("");
        await Assert.That(bad.Count).IsEqualTo(1);
        await Assert.That(bad[0].Constraint).IsEqualTo("cvc-complex-type.2.1");
    }

    [Test]
    public async Task Lax_WithoutDeclaration_IsNotAssessed()
    {
        var issues = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Lax, out _),
            "<r><unknown a=\"1\">text<child/></unknown></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Lax_WithDeclaration_AssessesTheElement()
    {
        var issues = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Lax, out _),
            "<r><known>text</known></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.1");
    }

    [Test]
    public async Task Lax_ChildrenOfAnUnassessedElement_AreAssessedLax()
    {
        var issues = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Lax, out _),
            "<r><unknown><known>text</known><other/></unknown></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.1");
    }

    [Test]
    public async Task Skip_NeverAssesses_EvenWhenADeclarationExistsAndItsContentIsInvalid()
    {
        var issues = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Skip, out _),
            "<r><known a=\"1\">text<x/></known><unknown><known>text</known></unknown></r>"
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Skip_FiresNoListenerHookForTheSkippedSubtree()
    {
        var listener = new RecordingListener();

        _ = ValidationHarness.Run(
            WildcardRoot("##any", XsdProcessContents.Skip, out _),
            "<r><known a=\"1\">text</known></r>",
            null,
            listener
        );

        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(string.Join(" | ", "begin", "start r#1@1 line 1", "end #1", "endrun"));
    }

    [Test]
    public async Task Wildcard_DoesNotMatchANamespaceItsConstraintExcludes()
    {
        var issues = ValidationHarness.Run(WildcardRoot("urn:allowed", XsdProcessContents.Skip, out _), "<r><x/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-complex-type.2.4");
    }

    [Test]
    public async Task ElementDeclaration_WinsOverTheWildcardInTheSameContentModel()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(
                    SchemaFixture.Part("a", SchemaFixture.Simple("int")),
                    SchemaFixture.Part(SchemaFixture.Wild("##any", XsdProcessContents.Skip), 0, long.MaxValue)
                )
            )
        );

        var issues = ValidationHarness.Run(schema, "<r><a>x</a><b/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-datatype-valid"));
    }
}
