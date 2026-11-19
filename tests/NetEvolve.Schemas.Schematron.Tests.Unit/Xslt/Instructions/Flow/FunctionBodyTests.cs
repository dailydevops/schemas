namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// The flow instructions inside a real <c>xsl:function</c> body, called through the function caller: the message channel of the
/// caller, the problems that an instruction records on the frame, and the caps of the frame.
/// </summary>
public class FunctionBodyTests
{
    [Test]
    public async Task Message_InAFunctionBody_ReachesTheChannelOfTheCaller_AndTheCallReturnsItsResult()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Fx.Function("f:m", null, 10, Message("'hello'", null, 11), Fx.Sequence("42", 12)));
        var messages = new RecordingMessages();
        var (_, caller) = fixture.CreateRun(messages: messages);
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "m"), 0, out var function);

        var status = caller.TryCall(function, [], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Integer(result)).IsEqualTo(42);
        await Assert.That(messages.Texts.Count).IsEqualTo(1);
        await Assert.That(messages.Texts[0]).IsEqualTo("hello");
    }

    [Test]
    public async Task Message_Terminate_InAFunctionBody_FailsTheCallWithXtmm9000()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Fx.Function("f:m", null, 10, Message("'stop'", "yes", 11), Fx.Sequence("42", 12)));
        var messages = new RecordingMessages();
        var (_, caller) = fixture.CreateRun(messages: messages);
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "m"), 0, out var function);

        var status = caller.TryCall(function, [], out _);

        var problem = caller.GetProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Constraint).IsEqualTo("XTMM9000");
        await Assert.That(problem.Message).Contains("stop");
        await Assert.That(messages.Texts.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_WithoutAChannel_IsDroppedInAFunctionBody()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Fx.Function("f:m", null, 10, Message("'quiet'", null, 11), Fx.Sequence("1", 12)));
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "m"), 0, out var function);

        await Assert.That(caller.TryCall(function, [], out _)).IsEqualTo(QueryStatus.Ok);
    }

    [Test]
    public async Task Sort_TypeError_InAFunctionBody_ReachesTheCallerWithItsOwnCode()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Fx.Function(
                "f:s",
                null,
                10,
                ForEach("(1, 2)", 11, Sort("if(. = 1) then 'a' else 1", null, 12), Fx.Sequence(".", 13))
            )
        );
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "s"), 0, out var function);

        var status = caller.TryCall(function, [], out _);

        var problem = caller.GetProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Constraint).IsEqualTo("XTDE1030");
        await Assert.That(problem.Location.Line).IsEqualTo(12);
    }

    [Test]
    public async Task Sort_TypeError_ThenARunProblem_ReportsEachWithItsOwnCode()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Fx.Function(
                "f:s",
                null,
                10,
                ForEach("(1, 2)", 11, Sort("if(. = 1) then 'a' else 1", null, 12), Fx.Sequence(".", 13))
            ),
            Fx.Function("f:r", null, 20, Fx.Sequence("1 idiv 0", 21))
        );
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "s"), 0, out var first);
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "r"), 0, out var second);

        _ = caller.TryCall(first, [], out _);
        var firstConstraint = caller.GetProblem().Constraint;
        _ = caller.TryCall(second, [], out _);

        await Assert.That(firstConstraint).IsEqualTo("XTDE1030");
        await Assert.That(caller.GetProblem().Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Frame_Caps_AreGuardedInsideTheirCeilings()
    {
        using var run = new MiniRun();
        var frame = new XsltFrame(run);

        await Assert.That(() => frame.MaxSequenceLength = 0).Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => frame.MaxSequenceLength = XsltFrame.MaxSequenceLengthCeiling + 1)
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => frame.MaxMessageLength = -1).Throws<ArgumentOutOfRangeException>();

        frame.MaxSequenceLength = XsltFrame.MaxSequenceLengthCeiling;

        await Assert.That(frame.MaxSequenceLength).IsEqualTo(XsltFrame.MaxSequenceLengthCeiling);
    }
}
