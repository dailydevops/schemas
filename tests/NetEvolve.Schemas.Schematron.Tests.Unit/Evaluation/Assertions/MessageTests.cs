namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>Message text: value-of, name, emph, dir, span, and a part that fails (design #150 EP-7 and EP-8, #137 AP-14).</summary>
public class MessageTests
{
    private static string MessageOf(AssertionHarness h, SchematronMessage message, string node = "order1")
    {
        var assertion = h.Compile(MakeAssert("false", message));
        _ = h.Evaluate(assertion, node);
        return h.Sink.Findings[0].Message;
    }

    [Test]
    public async Task ValueOf_InsertsTheEvaluatedText()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "7";

        var message = MessageOf(h, Message(Text("Order "), ValueOf("id"), Text(" is invalid")));

        await Assert.That(message).IsEqualTo("Order 7 is invalid");
    }

    [Test]
    public async Task ValueOf_AtomicItem_InsertsItsString()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["n"] = [XdmItem.FromInteger(42)];

        var message = MessageOf(h, Message(Text("n="), ValueOf("n")));

        await Assert.That(message).IsEqualTo("n=42");
    }

    [Test]
    public async Task Message_ValueOf_MultiItem_UsesSingleSpaceSeparator()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["pair"] = [XdmItem.FromInteger(1), XdmItem.FromInteger(2)];

        var message = MessageOf(h, Message(ValueOf("pair")));

        await Assert.That(message).IsEqualTo("1 2");
    }

    [Test]
    public async Task Message_ValueOf_Nodes_AreJoinedBySingleSpaces()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["items"] = [AssertionHarness.NodeItem("item1"), AssertionHarness.NodeItem("item2")];

        var message = MessageOf(h, Message(ValueOf("items")));

        await Assert.That(message).IsEqualTo("1 2");
    }

    [Test]
    public async Task ValueOf_TwoInstructions_AreNotSeparated()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["a"] = [XdmItem.FromInteger(1)];
        h.Binding.Items["b"] = [XdmItem.FromInteger(2)];

        var message = MessageOf(h, Message(ValueOf("a"), ValueOf("b")));

        await Assert.That(message).IsEqualTo("12");
    }

    [Test]
    public async Task ValueOf_EmptySequence_InsertsNothing()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message(Text("["), ValueOf("none"), Text("]")));

        await Assert.That(message).IsEqualTo("[]");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("order1", "order")]
    [Arguments("px", "p:x")]
    [Arguments("id", "id")]
    [Arguments("pi", "pi")]
    [Arguments("txt", "")]
    [Arguments("c", "")]
    [Arguments("doc", "")]
    public async Task Name_WithoutPath_InsertsTheNameOfTheContextNode(string node, string expected)
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message(Name()), node);

        await Assert.That(message).IsEqualTo(expected);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false"]);
    }

    [Test]
    public async Task Name_WithPath_UsesThePathResult_NotTheContextNode()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["../px"] = [AssertionHarness.NodeItem("px")];

        var message = MessageOf(h, Message(Text("see "), Name("../px")), "order1");

        await Assert.That(message).IsEqualTo("see p:x");
    }

    [Test]
    public async Task Name_WithPath_EmptyResult_InsertsNothing()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message(Text("["), Name("missing"), Text("]")));

        await Assert.That(message).IsEqualTo("[]");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Name_WithPath_MoreThanOneNode_ReportsAProblem_AndEndsTheText()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["items"] = [AssertionHarness.NodeItem("item1"), AssertionHarness.NodeItem("item2")];

        var message = MessageOf(h, Message(Text("n="), Name("items"), Text(" end")));

        await Assert.That(message).IsEqualTo("n=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Name_WithPath_NonNodeResult_ReportsAProblem_AndEndsTheText()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["text"] = [XdmItem.FromString("x")];

        var message = MessageOf(h, Message(Text("n="), Name("text"), Text(" end")));

        await Assert.That(message).IsEqualTo("n=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Name_WithPath_ReadingAFailedSlot_ReportsNoSecondProblem_AndEndsTheText()
    {
        using var h = new AssertionHarness(variables: ["a"]);
        h.Binder.Failing.Add(0);

        var message = MessageOf(h, Message(Text("n="), Name("$a"), Text(" end")));

        await Assert.That(message).IsEqualTo("n=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false"]);
    }

    [Test]
    public async Task Name_WithPath_DynamicError_ReportsOneProblem_AndKeepsTheTextBuiltBefore()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message(Text("n="), Name("bad"), Text(" end")));

        await Assert.That(message).IsEqualTo("n=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Name_WithPath_TextResult_ReportsAProblem_AndEndsTheText()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["text"] = "x";

        var message = MessageOf(h, Message(Text("n="), Name("text"), Text(" end")));

        await Assert.That(message).IsEqualTo("n=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Name_WithPath_EmptyTextResult_InsertsNothing()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["text"] = "";

        var message = MessageOf(h, Message(Text("["), Name("text"), Text("]")));

        await Assert.That(message).IsEqualTo("[]");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EmphDirAndSpan_ContributeTheirTextWithoutMarkup()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(
            h,
            Message(Text("a "), Emph(Text("b")), Dir("rtl", Text(" c")), Span("k", Text(" d")), Text("."))
        );

        await Assert.That(message).IsEqualTo("a b c d.");
    }

    [Test]
    public async Task EmphDirAndSpan_AreKeptInTheStructuredMessage()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "7";
        var assertion = h.Compile(
            MakeAssert("false", Message(Text("a "), Emph(Text("b")), Dir("rtl", Text(" c")), Span("k", ValueOf("id"))))
        );

        var parts = assertion.Message.Parts;

        await Assert.That(parts.Length).IsEqualTo(4);
        await Assert.That(parts[0].Kind).IsEqualTo(MessagePartKind.Text);
        await Assert.That(parts[0].Text).IsEqualTo("a ");
        await Assert.That(parts[1].Kind).IsEqualTo(MessagePartKind.Emphasis);
        await Assert.That(parts[1].Children.Length).IsEqualTo(1);
        await Assert.That(parts[1].Children[0].Text).IsEqualTo("b");
        await Assert.That(parts[2].Kind).IsEqualTo(MessagePartKind.Direction);
        await Assert.That(parts[2].Attribute).IsEqualTo("rtl");
        await Assert.That(parts[3].Kind).IsEqualTo(MessagePartKind.Span);
        await Assert.That(parts[3].Attribute).IsEqualTo("k");
        await Assert.That(parts[3].Children[0].Kind).IsEqualTo(MessagePartKind.ValueOf);
    }

    [Test]
    public async Task EmphDirAndSpan_EvaluateTheirNestedParts()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "7";

        var message = MessageOf(h, Message(Emph(Text("id "), Span(null, ValueOf("id"), Name()))));

        await Assert.That(message).IsEqualTo("id 7order");
    }

    [Test]
    public async Task Text_IsKeptAsWritten_WhitespaceIncluded()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message("  a \n  b  "));

        await Assert.That(message).IsEqualTo("  a \n  b  ");
    }

    [Test]
    public async Task Message_Error_TruncatesAtTheFailingPart_KeepsTheFinding()
    {
        using var h = new AssertionHarness();

        var message = MessageOf(h, Message(Text("v="), ValueOf("bad"), Text(" end")));

        await Assert.That(message).IsEqualTo("v=");
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_WithoutError_IsComplete_AndReportsNothing()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["n"] = [XdmItem.FromInteger(7)];

        var message = MessageOf(h, Message(Text("v="), ValueOf("n"), Text(" end")));

        await Assert.That(message).IsEqualTo("v=7 end");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Message_SlotsAreBoundBeforeEveryEvaluation_ReadingAFailedSlotAddsNoProblem()
    {
        using var h = new AssertionHarness(variables: ["a", "b"]);
        h.Binding.Texts["$a"] = "A";
        h.Binding.Texts["$b"] = "B";
        h.Binder.Failing.Add(1);

        var message = MessageOf(h, Message(Text("x"), ValueOf("$a"), ValueOf("$b"), Text("y")));

        await Assert.That(message).IsEqualTo("xA");
        await Assert.That(h.Binder.Bound).IsEquivalentTo([0, 1]);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false", "$a"]);
    }

    [Test]
    public async Task Message_BeyondTheMaximum_IsCut_AndReported()
    {
        using var h = new AssertionHarness(maxTextLength: 3);

        var message = MessageOf(h, Message("abcdef"));

        await Assert.That(message).IsEqualTo("abc");
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Message_AtomicValueWithoutATextForm_ReportsALimitation_AndEndsTheText()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["d"] = [XdmItem.FromDouble(1.5)];

        var message = MessageOf(h, Message(Text("d="), ValueOf("d"), Text(" end")));

        await Assert.That(message).IsEqualTo("d=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Message_ValueOf_FailingAfterSomeItems_LeavesNoPartialText()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["partial"] = [XdmItem.FromString("a"), XdmItem.FromString("b")];
        h.Binding.FailAfterPush.Add("partial");

        var message = MessageOf(h, Message(Text("d="), ValueOf("partial"), Text(" end")));

        await Assert.That(message).IsEqualTo("d=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_ValueOf_UnconvertibleItemMidSequence_LeavesNoPartialText()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["mixed"] = [XdmItem.FromString("a"), XdmItem.FromDouble(1.5), XdmItem.FromString("b")];

        var message = MessageOf(h, Message(Text("d="), ValueOf("mixed"), Text(" end")));

        await Assert.That(message).IsEqualTo("d=");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_LongerThan256Characters_IsBuiltWhole()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "tail";
        var head = new string('a', 300);

        var message = MessageOf(h, Message(Text(head), ValueOf("id")));

        await Assert.That(message).IsEqualTo(head + "tail");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Builder_BuildsIntoTheGivenSink_AndReportsTheStatus()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "7";
        var ok = h.Compile(MakeAssert("false", Message(Text("a"), ValueOf("id")))).Message;
        var failing = h.Compile(MakeAssert("false", Message(Text("b"), ValueOf("bad")))).Message;
        var builder = new MessageBuilder(h.Run, h.Binder, h.Problems);
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);
        using var sink = new TextSink(100);

        var first = builder.Build(ok, in focus, sink);
        var second = builder.Build(failing, in focus, sink);

        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(sink.ToString()).IsEqualTo("a7b");
    }

    [Test]
    public async Task Builder_NullArguments_Throw()
    {
        using var h = new AssertionHarness();
        var message = h.Compile(MakeAssert("false", Message("m"))).Message;
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);
        var builder = new MessageBuilder(h.Run, h.Binder, h.Problems);
        using var sink = new TextSink(10);

        await Assert.That(() => new MessageBuilder(null!, h.Binder, h.Problems)).Throws<ArgumentNullException>();
        await Assert.That(() => new MessageBuilder(h.Run, null!, h.Problems)).Throws<ArgumentNullException>();
        await Assert.That(() => new MessageBuilder(h.Run, h.Binder, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.Build(null!, in focus, sink)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.Build(message, in focus, null!)).Throws<ArgumentNullException>();
    }
}
