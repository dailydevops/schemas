namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// <c>xsl:message</c> (XSLT 2.0 section 17): the message goes to the diagnostic channel of the frame and evaluation continues;
/// <c>terminate="yes"</c> ends the evaluation with the coded error XTMM9000.
/// </summary>
public class MessageTests
{
    [Test]
    public async Task Message_WithoutTerminate_GoesToTheChannel_AndEvaluationContinues()
    {
        var harness = new FlowHarness(
            ValueOf(null, null, 9, Message(null, null, 10, ValueOf("'careful'", null, 11)), Seq("'after'", 12))
        );

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Messages.Texts.Count).IsEqualTo(1);
        await Assert.That(harness.Messages.Texts[0]).IsEqualTo("careful");
        await Assert.That(harness.Messages.Locations[0].Line).IsEqualTo(10);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_ProducesNoOutput_ItsResultIsTheEmptySequence()
    {
        var harness = new FlowHarness(Message("'x'", "no", 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
        await Assert.That(harness.Messages.Texts[0]).IsEqualTo("x");
    }

    [Test]
    public async Task Message_Terminate_IsTheCodedErrorXtmm9000_AfterTheMessageWasSent()
    {
        var harness = new FlowHarness(Message("'stop here'", "yes", 10));

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XTMM9000");
        await Assert.That(problem.Message).Contains("stop here");
        await Assert.That(problem.Location.Line).IsEqualTo(10);
        await Assert.That(harness.Messages.Texts.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Message_Terminate_StopsTheInstructionsAfterIt()
    {
        var harness = new FlowHarness(
            ValueOf(null, null, 9, Seq("'before'", 10), Message("'x'", "yes", 11), Seq("'after'", 12))
        );

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Sink.Joined()).DoesNotContain("after");
    }

    [Test]
    public async Task Message_SelectAndContent_AreOneMessage_SelectFirst()
    {
        var harness = new FlowHarness(Message("'a'", null, 10, ValueOf("'b'", null, 11)));

        _ = harness.Execute();

        await Assert.That(harness.Messages.Texts[0]).IsEqualTo("ab");
    }

    [Test]
    public async Task Message_AdjacentAtomicValues_AreSeparatedBySpace()
    {
        var harness = new FlowHarness(Message("(1, 2, 3)", null, 10));

        _ = harness.Execute();

        await Assert.That(harness.Messages.Texts[0]).IsEqualTo("1 2 3");
    }

    [Test]
    public async Task Message_WithoutSelectAndContent_IsTheEmptyMessage()
    {
        var harness = new FlowHarness(Message(null, null, 10));

        _ = harness.Execute();

        await Assert.That(harness.Messages.Texts[0]).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Message_WithoutAChannel_IsDropped_AndTerminateStillStops()
    {
        var quiet = new FlowHarness(Message("'x'", null, 10));
        var stop = new FlowHarness(Message("'x'", "yes", 10));
        quiet.Frame.Messages = null;
        stop.Frame.Messages = null;

        await Assert.That(quiet.Execute()).IsEqualTo(QueryStatus.Ok);
        await Assert.That(stop.Execute()).IsEqualTo(QueryStatus.Problem);
    }

    [Test]
    public async Task Message_FailingSelect_SendsNothing_AndReturnsTheProblem()
    {
        var harness = new FlowHarness(Message("1 idiv 0", null, 10));

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Messages.Texts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Message_InvalidTerminate_IsStaticErrorXtse0020()
    {
        var harness = new FlowHarness(Message("'x'", "maybe", 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3006/XTSE0020");
    }

    [Test]
    public async Task Message_TerminateTemplate_IsUnsupported()
    {
        var harness = new FlowHarness(Message("'x'", "{$t}", 10), "t");

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Message_LongerThanTheLimit_IsTheCodedLimitError()
    {
        var harness = new FlowHarness(Message("'abcdef'", null, 10));
        harness.Frame.MaxMessageLength = 4;

        var status = harness.Execute();

        var problem = harness.Frame.TakeProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
        await Assert.That(harness.Messages.Texts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Message_ReportsTheSlotsOfSelectAndContent()
    {
        var harness = new FlowHarness(Message("$a", null, 10, ValueOf("$b", null, 11)), "a", "b", "c");

        await Assert.That(((IXsltSlotReader)harness.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([0, 1]);
    }
}
