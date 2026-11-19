namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// The focus of an iteration (design #150 EP-13 and RD-7): the sequence focus of <c>xsl:for-each</c> has the size -1 until
/// <c>last()</c> asks for it, the focus can hold an atomic item, and a function body never sees the focus of its caller.
/// </summary>
public class FocusTests
{
    [Test]
    public async Task QueryFocus_Size_IsMinusOneUntilRequested()
    {
        var harness = new FlowHarness(ForEach("(10, 20, 30)", 11, ValueOf("last()", null, 12)));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Sink.Joined()).IsEqualTo("333");
        for (var i = 1; i <= 3; i++)
        {
            var focus = harness.Run.Focuses[i];
            await Assert.That(focus.Size).IsEqualTo(-1);
        }
    }

    [Test]
    public async Task QueryFocus_ResolveSize_PrefersAKnownSize_AndIsMinusOneWithoutASource()
    {
        var known = QueryFocus.ForItem(XdmItem.FromInteger(1), 1, 7);
        var unknown = QueryFocus.ForItem(XdmItem.FromInteger(1), 1, -1);

        await Assert.That(known.ResolveSize()).IsEqualTo(7);
        await Assert.That(unknown.ResolveSize()).IsEqualTo(-1);
    }

    [Test]
    public async Task QueryFocus_AtomicItem_HasNoNode_AndANodeFocusHasItsItem()
    {
        var atomic = QueryFocus.ForItem(XdmItem.FromInteger(5), 2, 3);
        var element = SinkTestSupport.Element("a", "x");
        _ = element.TryGetNode(out var node);
        var nodeFocus = new QueryFocus(node, 1, 1);
        var absent = default(QueryFocus);

        await Assert.That(atomic.Item.TryGetInteger(out var five) ? five : -1).IsEqualTo(5);
        await Assert.That(atomic.Node.Tree).IsNull();
        await Assert.That(nodeFocus.Item.IsNode).IsTrue();
        await Assert.That(nodeFocus.Node.IsSameNode(in node)).IsTrue();
        await Assert.That(absent.Item.Kind).IsEqualTo(XdmItemKind.None);
    }

    [Test]
    public async Task QueryFocus_ForItem_OfANode_SetsTheNodeToo()
    {
        var element = SinkTestSupport.Element("a", "x");
        _ = element.TryGetNode(out var node);

        var focus = QueryFocus.ForItem(element, 1, 1);

        await Assert.That(focus.Node.IsSameNode(in node)).IsTrue();
    }

    [Test]
    public async Task Frame_StartsWithoutAFocus_AndAFocusCanBeSet()
    {
        using var run = new MiniRun();
        var frame = new XsltFrame(run);

        await Assert.That(frame.Focus.Item.Kind).IsEqualTo(XdmItemKind.None);

        frame.Focus = QueryFocus.ForItem(XdmItem.FromInteger(1), 1, 1);

        await Assert.That(frame.Focus.Position).IsEqualTo(1);
    }

    [Test]
    public async Task Frame_Fail_IsReadOnceThroughTakeProblem_ThenTheRunProblemShowsAgain()
    {
        var harness = new FlowHarness(ValueOf("1 idiv 0", null, 10));
        var own = new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, "own", default, "XTMM9000");

        var status = harness.Frame.Fail(in own);
        var first = harness.Frame.TakeProblem();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(first.Constraint).IsEqualTo("XTMM9000");

        // A later failure of the run itself must not be masked by the earlier problem of the frame.
        await Assert.That(harness.Execute()).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Frame.TakeProblem().Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Function_CalledFromAForEachBody_HasNoFocus()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Fx.Function("f:probe", null, 10, Fx.Sequence(".", 11)),
            Fx.Function("f:outer", null, 20, ForEach("5", 21, Fx.Sequence("f:probe()", 22)))
        );
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(
            new NetEvolve.Schemas.XPath.Api.XPathQName(FunctionTree.Functions, "outer"),
            0,
            out var outer
        );

        var status = caller.TryCall(outer, [], out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(caller.GetProblem().Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Function_CalledFromAForEachBody_LeavesTheFocusOfTheLoopAsItWas()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Fx.Function("f:id", null, 10, Fx.Param("a", null, 11), Fx.Sequence("$a", 12)),
            Fx.Function("f:outer", null, 20, ForEach("5", 21, Fx.Sequence("(f:id(1), .)", 22)))
        );
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(
            new NetEvolve.Schemas.XPath.Api.XPathQName(FunctionTree.Functions, "outer"),
            0,
            out var outer
        );

        var status = caller.TryCall(outer, [], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(MiniQuery.Integer(XdmSequence.Create(result[1]))).IsEqualTo(5);
    }
}
