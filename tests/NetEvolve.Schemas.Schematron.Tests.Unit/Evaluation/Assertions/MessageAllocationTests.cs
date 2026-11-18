namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>Building a message uses a pooled buffer and no intermediate string: only the final text is allocated.</summary>
public class MessageAllocationTests
{
    private static CompiledMessage RichMessage(AssertionHarness h)
    {
        h.Binding.Record = false;
        h.Binding.Texts["id"] = "7";
        h.Binding.Items["ids"] = [AssertionHarness.NodeItem("item1"), AssertionHarness.NodeItem("item2")];
        h.Binding.Items["count"] = [XdmItem.FromInteger(12345)];
        var assertion = h.Compile(
            MakeAssert(
                "false",
                Message(
                    Text("Order "),
                    ValueOf("id"),
                    Text(" in "),
                    Name(),
                    Text(" has "),
                    Emph(ValueOf("ids")),
                    Span("k", Text(" and "), ValueOf("count")),
                    Dir("rtl", Text("."))
                )
            )
        );
        return assertion.Message;
    }

    [Test]
    public async Task Message_Build_AllocatesOnlyTheFinalText()
    {
        using var h = new AssertionHarness();
        var message = RichMessage(h);
        var builder = new MessageBuilder(h.Run, h.Binder, h.Problems);
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);
        using var sink = new TextSink(1000);
        _ = builder.Build(message, in focus, sink);
        var length = sink.Written.Length;
        var expected = sink.ToString();
        var finalText = AllocationAssert.Measure(() => _ = new string('x', length));
        string? built = null;

        AllocationAssert.AtMost(finalText, () =>
        {
            sink.Clear();
            _ = builder.Build(message, in focus, sink);
            built = sink.ToString();
        });

        await Assert.That(built).IsEqualTo(expected);
        await Assert.That(expected).IsEqualTo("Order 7 in order has 1 2 and 12345.");
        await Assert.That(finalText).IsGreaterThan(0L);
    }

    [Test]
    public async Task Message_Build_IntoAReusedSink_AllocatesNothing()
    {
        using var h = new AssertionHarness();
        var message = RichMessage(h);
        var builder = new MessageBuilder(h.Run, h.Binder, h.Problems);
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);
        using var sink = new TextSink(1000);

        AllocationAssert.NoAllocation(() =>
        {
            sink.Clear();
            _ = builder.Build(message, in focus, sink);
        });

        await Assert.That(sink.Written.Length).IsGreaterThan(0);
    }
}
