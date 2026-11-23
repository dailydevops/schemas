namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// <c>xsl:for-each</c> (XSLT 2.0 section 8.1, design #150 EP-8, EP-13, and EP-16): the body runs once per item with the focus (item,
/// position, size), the items come in sorted order when sort keys are present, and every iteration is a cancellation point.
/// </summary>
public class ForEachTests
{
    private const string Describe = "concat(position(), '/', last(), ':', .)";

    private static string Run(FlowHarness harness)
    {
        var status = harness.Execute();
        return status == QueryStatus.Ok ? harness.Sink.Joined() : "problem";
    }

    [Test]
    public async Task ForEach_SetsTheFocus_ItemPositionAndSize()
    {
        var harness = new FlowHarness(ForEach("(10, 20, 30)", 10, ValueOf(Describe, null, 11)));

        await Assert.That(Run(harness)).IsEqualTo("1/3:102/3:203/3:30");
    }

    [Test]
    public async Task ForEach_NodeItems_AreTheContextNodeByReference()
    {
        var a = SinkTestSupport.Element("a", "x");
        var b = SinkTestSupport.Element("b", "y");
        var harness = new FlowHarness(ForEach("$nodes", 10, ValueOf("'.'", null, 11)), "nodes");
        harness.Bind("nodes", XdmSequence.Create([a, b]));

        _ = harness.Execute();

        // The first evaluation is the select, the later ones are the iterations.
        var focuses = harness.Run.Focuses;
        await Assert.That(focuses.Count).IsEqualTo(3);
        _ = a.TryGetNode(out var first);
        await Assert.That(focuses[1].Node.IsSameNode(in first)).IsTrue();
        await Assert.That(focuses[2].Position).IsEqualTo(2);
        await Assert.That(focuses[2].Item.IsNode).IsTrue();
    }

    [Test]
    public async Task ForEach_SelectIsEvaluatedWithTheFocusOfTheInstruction()
    {
        var harness = new FlowHarness(ForEach("(1, 2)", 10, ValueOf("'x'", null, 11)));
        harness.Frame.Focus = QueryFocus.ForItem(XdmItem.FromInteger(7), 4, 9);

        _ = harness.Execute();

        await Assert.That(harness.Run.Focuses[0].Position).IsEqualTo(4);
        await Assert.That(harness.Run.Focuses[0].Size).IsEqualTo(9);
    }

    [Test]
    public async Task ForEach_EmptySequence_RunsTheBodyNever()
    {
        var harness = new FlowHarness(ForEach("()", 10, ValueOf("'x'", null, 11)));

        await Assert.That(Run(harness)).IsEqualTo(string.Empty);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ForEach_RestoresTheOuterFocus_AfterTheLoopAndAfterAFailure()
    {
        var done = new FlowHarness(ForEach("(1, 2)", 10, ValueOf("'x'", null, 11)));
        var failing = new FlowHarness(ForEach("(1, 2)", 10, ValueOf("1 idiv 0", null, 11)));
        var outer = QueryFocus.ForItem(XdmItem.FromInteger(7), 3, 5);
        done.Frame.Focus = outer;
        failing.Frame.Focus = outer;

        _ = done.Execute();
        var status = failing.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(done.Frame.Focus.Position).IsEqualTo(3);
        await Assert.That(failing.Frame.Focus.Position).IsEqualTo(3);
        await Assert.That(failing.Frame.Focus.Item.TryGetInteger(out var kept) ? kept : -1).IsEqualTo(7);
    }

    [Test]
    public async Task ForEach_Nested_EachLoopHasItsOwnFocus()
    {
        var harness = new FlowHarness(
            ForEach(
                "(1, 2)",
                10,
                ForEach("(10, 20)", 11, ValueOf("concat(., '@', position(), '/', last(), ';')", null, 12)),
                ValueOf("concat('|', position(), '/', last(), ';')", null, 13)
            )
        );

        await Assert.That(Run(harness)).IsEqualTo("10@1/2;20@2/2;|1/2;10@1/2;20@2/2;|2/2;");
    }

    [Test]
    public async Task ForEach_FailingBody_StopsAtTheFirstProblem()
    {
        var harness = new FlowHarness(
            ForEach("(1, 2, 3)", 10, If("(. = 2)", 11, ValueOf("1 idiv 0", null, 12)), ValueOf(".", null, 13))
        );

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Sink.Joined()).IsEqualTo("1");
    }

    [Test]
    public async Task ForEach_FailingSelect_ReturnsTheProblem()
    {
        var harness = new FlowHarness(ForEach("1 idiv 0", 10, ValueOf("'x'", null, 11)));

        await Assert.That(Run(harness)).IsEqualTo("problem");
    }

    [Test]
    public async Task ForEach_LocalVariableInTheBody_IsBoundPerIteration()
    {
        var harness = new FlowHarness(ForEach("(1, 2, 3)", 10, Var("sq", ". * .", 11), ValueOf("$sq", null, 12)));

        await Assert.That(Run(harness)).IsEqualTo("149");
    }

    [Test]
    public async Task ForEach_EveryIteration_IsACancellationPoint()
    {
        using var source = new CancellationTokenSource();
        var harness = new FlowHarness(ForEach("(1, 2, 3)", 10, ValueOf("'x'", null, 11)), source.Token);
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert.That(() => harness.Execute()).Throws<System.OperationCanceledException>();
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ForEach_MoreItemsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new FlowHarness(ForEach("(1, 2, 3, 4)", 10, ValueOf("'x'", null, 11)));
        harness.Frame.MaxSequenceLength = 3;

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ForEach_ExactlyTheSequenceLimit_Runs()
    {
        var harness = new FlowHarness(ForEach("(1, 2, 3)", 10, ValueOf(".", null, 11)));
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(Run(harness)).IsEqualTo("123");
    }

    [Test]
    public async Task ForEach_WithoutSelect_IsStaticErrorMissingAttribute()
    {
        var harness = new FlowHarness(ForEach(null, 10, ValueOf("'x'", null, 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3005");
    }

    [Test]
    public async Task ForEach_ReportsTheSlotsOfSelectSortAndBody()
    {
        var harness = new FlowHarness(
            ForEach("$a", 10, Sort("$b", null, 11), ValueOf("$c", null, 12)),
            "a",
            "b",
            "c",
            "d"
        );

        await Assert.That(((IXsltSlotReader)harness.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([0, 1, 2]);
    }
}
