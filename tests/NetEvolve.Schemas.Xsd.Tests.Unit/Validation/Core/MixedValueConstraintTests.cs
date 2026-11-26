namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Xsd.Elements;

public class MixedValueConstraintTests
{
    private static SchemaFixture Schema(ElementValueConstraint constraint)
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.Mixed(SchemaFixture.Seq(SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, long.MaxValue))),
            constraint
        );
        return schema;
    }

    private static ElementValueConstraint Default(string value) => new(ElementValueConstraintKind.Default, value);

    private static ElementValueConstraint Fixed(string value) => new(ElementValueConstraintKind.Fixed, value);

    [Test]
    [Arguments("<r/>", true)]
    [Arguments("<r></r>", true)]
    [Arguments("<r>mine</r>", false)]
    [Arguments("<r><a/></r>", false)]
    public async Task Default_IsAppliedOnlyToAnElementWithoutChildrenAndText(string xml, bool applied)
    {
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(Schema(Default("dflt")), xml, null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(listener.Events.Contains("value dflt (default)")).IsEqualTo(applied);
    }

    [Test]
    public async Task Fixed_IsAppliedToAnEmptyElement()
    {
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(Schema(Fixed("one")), "<r/>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(listener.Events).Contains("value one (default)");
    }

    [Test]
    public async Task Fixed_MatchingText_IsValid()
    {
        var issues = ValidationHarness.Run(Schema(Fixed("one")), "<r>one</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixed_WithAnotherText_IsAnError()
    {
        var issues = ValidationHarness.Run(Schema(Fixed("one")), "<r>two</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-elt.5.2.2.2.1");
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-elt"));
    }

    [Test]
    [Arguments("<r><a/></r>")]
    [Arguments("<r>one<a/></r>")]
    public async Task Fixed_WithAChildElement_IsAnError(string xml)
    {
        var issues = ValidationHarness.Run(Schema(Fixed("one")), xml);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-elt.5.2.2.1");
    }

    [Test]
    public async Task Fixed_TextSplitByAComment_IsCompared()
    {
        var issues = ValidationHarness.Run(Schema(Fixed("onetwo")), "<r>one<!-- c -->two</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
