namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// <c>xsl:value-of</c> (XSLT 2.0 section 11.4.3, design #150 EP-8): the instruction joins its own items with the separator and pushes
/// text, so the same instruction runs unchanged into every kind of sink.
/// </summary>
public class ValueOfTests
{
    [Test]
    public async Task ValueOf_PushesOneTextNode_IntoAnItemSink()
    {
        var harness = new FlowHarness(ValueOf("(1, 2)", null, 10));
        var sink = new ItemSequenceSink(SinkTestSupport.NewTable());

        var status = harness.Execute(sink);

        var result = sink.Result;
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(1);
        var node = SinkTestSupport.Node(result[0]);
        await Assert.That(node.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(node.StringValue).IsEqualTo("1 2");
    }

    [Test]
    public async Task ValueOf_Select_DefaultSeparatorIsOneSpace()
    {
        var harness = new FlowHarness(ValueOf("(1, 2, 3)", null, 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.All()).IsEqualTo("text:1|text: |text:2|text: |text:3");
    }

    [Test]
    public async Task ValueOf_SeparatorAttribute_IsHonored()
    {
        var harness = new FlowHarness(ValueOf("(1, 2, 3)", "|", 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("1|2|3");
    }

    [Test]
    public async Task ValueOf_EmptySeparator_JoinsWithoutAnything()
    {
        var harness = new FlowHarness(ValueOf("(1, 2, 3)", string.Empty, 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("123");
    }

    [Test]
    public async Task ValueOf_Separator_IsNotAddedBeforeTheFirstOrAfterTheLastItem()
    {
        var harness = new FlowHarness(ValueOf("'only'", "-", 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("only");
    }

    [Test]
    public async Task ValueOf_Content_DefaultSeparatorIsEmpty()
    {
        var harness = new FlowHarness(ValueOf(null, null, 10, Seq("(1, 2)", 11)));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("12");
    }

    [Test]
    public async Task ValueOf_Content_HonorsTheSeparator()
    {
        var harness = new FlowHarness(ValueOf(null, ", ", 10, Seq("(1, 2)", 11), Seq("'x'", 12)));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("1, 2, x");
    }

    [Test]
    public async Task ValueOf_NodeItems_ContributeTheirStringValue()
    {
        var harness = new FlowHarness(ValueOf("$nodes", "+", 10), "nodes");
        harness.Bind(
            "nodes",
            XdmSequence.Create([SinkTestSupport.Element("a", "x"), SinkTestSupport.Element("b", "y")])
        );

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("x+y");
    }

    [Test]
    public async Task ValueOf_EmptySequence_PushesNothing()
    {
        var harness = new FlowHarness(ValueOf("()", "-", 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ValueOf_BooleanAndString_UseTheirLexicalForm()
    {
        var harness = new FlowHarness(ValueOf("(true(), 'a')", "/", 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Joined()).IsEqualTo("true/a");
    }

    [Test]
    public async Task ValueOf_SelectAndContent_IsStaticErrorXtse0870()
    {
        var harness = new FlowHarness(ValueOf("1", null, 10, Seq("2", 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0870");
    }

    [Test]
    public async Task ValueOf_NeitherSelectNorContent_IsStaticErrorXtse0870()
    {
        var harness = new FlowHarness(ValueOf(null, null, 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3005/XTSE0870");
    }

    [Test]
    public async Task ValueOf_InvalidSelect_ReportsTheExpressionProblem()
    {
        var harness = new FlowHarness(ValueOf("1 +", null, 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3202/XPST0003");
    }

    [Test]
    public async Task ValueOf_FailingSelect_ReturnsTheProblemOfTheRun()
    {
        var harness = new FlowHarness(ValueOf("1 idiv 0", null, 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Frame.TakeProblem().Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task ValueOf_ItemWithoutATextForm_IsADynamicErrorOfTheInstruction()
    {
        var harness = new FlowHarness(ValueOf("$d", null, 10), "d");
        harness.Bind("d", XdmSequence.Create(XdmItem.FromDouble(1.5)));

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task ValueOf_ReportsTheSlotsOfItsSelectAndOfItsContent()
    {
        var select = new FlowHarness(ValueOf("$b", null, 10), "a", "b");
        var content = new FlowHarness(ValueOf(null, "$a", 10, Seq("$b", 11)), "a", "b");

        await Assert.That(((IXsltSlotReader)select.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([1]);
        await Assert.That(((IXsltSlotReader)content.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([1]);
    }

    [Test]
    public async Task ValueOf_SeparatorWithBraces_IsTheAttributeValueTemplateEscapeOrUnsupported()
    {
        var escaped = new FlowHarness(ValueOf("(1, 2)", "{{-}}", 10));
        var template = new FlowHarness(ValueOf("(1, 2)", "{$x}", 10), "x");
        var open = new FlowHarness(ValueOf("(1, 2)", "a{b", 10));
        var close = new FlowHarness(ValueOf("(1, 2)", "a}b", 10));

        _ = escaped.Execute();

        await Assert.That(escaped.Sink.Joined()).IsEqualTo("1{-}2");
        await Assert.That(template.CompileCodes).IsEqualTo("NES3204");
        await Assert.That(open.CompileCodes).IsEqualTo("NES3006/XTSE0350");
        await Assert.That(close.CompileCodes).IsEqualTo("NES3006/XTSE0370");
    }
}
