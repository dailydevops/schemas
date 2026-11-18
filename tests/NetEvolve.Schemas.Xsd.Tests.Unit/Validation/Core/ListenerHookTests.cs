namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.Xsd.Contracts;

public class ListenerHookTests
{
    private static SchemaFixture Schema()
    {
        var schema = new SchemaFixture();
        schema.Global(
            "r",
            schema.ElementOnly(
                SchemaFixture.Seq(SchemaFixture.Part("c", SchemaFixture.Simple("int"), 0, long.MaxValue)),
                schema.Use("a")
            )
        );
        return schema;
    }

    [Test]
    public async Task Hooks_FireInDocumentOrder_ElementStartAttributesSimpleValueElementEnd()
    {
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(Schema(), "<r a=\"1\">\n  <c>7</c>\n  <c>8</c>\n</r>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(
                string.Join(
                    " | ",
                    "begin",
                    "start r#1@1 line 1",
                    "attr a=1",
                    "start c#2@2 line 2",
                    "value 7",
                    "end #2",
                    "start c#3@2 line 3",
                    "value 8",
                    "end #3",
                    "end #1",
                    "endrun"
                )
            );
    }

    [Test]
    public async Task Hooks_ReportInvalidValues_AndTheInvalidityReachesTheAncestors()
    {
        var listener = new RecordingListener();

        _ = ValidationHarness.Run(Schema(), "<r a=\"1\"><c>x</c></r>", null, listener);

        await Assert.That(listener.Events).Contains("value x (invalid)");
        await Assert.That(listener.Events).Contains("end #2 (invalid)");
        await Assert.That(listener.Events).Contains("end #1 (invalid)");
    }

    [Test]
    public async Task Hooks_AFailedContentModelMakesTheElementInvalid()
    {
        var listener = new RecordingListener();

        _ = ValidationHarness.Run(Schema(), "<r a=\"1\"><z/></r>", null, listener);

        await Assert.That(listener.Events).Contains("end #1 (invalid)");
    }

    [Test]
    public async Task Hooks_UnassessedElement_FiresWithoutDeclaration_AndItsInvalidRootIsReported()
    {
        var listener = new RecordingListener();

        var issues = ValidationHarness.Run(new SchemaFixture(), "<other a=\"1\">text</other>", null, listener);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(string.Join(" | ", "begin", "start other#1@1 line 1", "attr a=1", "end #1 (invalid)", "endrun"));
    }

    [Test]
    public async Task Hooks_OutsideTheInterest_AreNeverCalled()
    {
        var listener = new RecordingListener(ListenerInterest.EndElement);

        _ = ValidationHarness.Run(Schema(), "<r a=\"1\"><c>7</c></r>", null, listener);

        await Assert
            .That(string.Join(" | ", listener.Events))
            .IsEqualTo(string.Join(" | ", "begin", "end #2", "end #1", "endrun"));
    }

    [Test]
    public async Task Listeners_AreCalledInTheGivenOrderPerEvent()
    {
        var log = new System.Collections.Generic.List<string>();
        var ordered = new OrderListener(log, "one");
        var other = new OrderListener(log, "two");

        _ = ValidationHarness.Run(Schema(), "<r/>", null, ordered, other);

        await Assert.That(string.Join(" | ", log)).IsEqualTo(string.Join(" | ", "one", "two"));
    }

    private sealed class OrderListener(System.Collections.Generic.List<string> log, string name) : ValidationListener
    {
        public override ListenerInterest Interest => ListenerInterest.StartElement;

        public override void StartElement(in ElementStartInfo info) => log.Add(name);
    }
}
