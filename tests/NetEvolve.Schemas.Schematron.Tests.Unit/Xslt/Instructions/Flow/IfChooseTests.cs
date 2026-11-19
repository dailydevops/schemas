namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary><c>xsl:if</c> (XSLT 2.0 section 8.2) and <c>xsl:choose</c> with <c>xsl:when</c> and <c>xsl:otherwise</c> (section 8.2).</summary>
public class IfChooseTests
{
    private static string Run(FlowHarness harness)
    {
        var status = harness.Execute();
        return status == QueryStatus.Ok ? harness.Sink.Joined() : "problem";
    }

    [Test]
    public async Task If_TrueTest_RunsTheBody()
    {
        var harness = new FlowHarness(If("1 = 1", 10, ValueOf("'yes'", null, 11)));

        await Assert.That(Run(harness)).IsEqualTo("yes");
    }

    [Test]
    public async Task If_FalseTest_RunsNothing()
    {
        var harness = new FlowHarness(If("1 = 2", 10, ValueOf("'yes'", null, 11)));

        await Assert.That(Run(harness)).IsEqualTo(string.Empty);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task If_TestIsTheEffectiveBooleanValue()
    {
        var emptyString = new FlowHarness(If("''", 10, ValueOf("'x'", null, 11)));
        var emptySequence = new FlowHarness(If("()", 10, ValueOf("'x'", null, 11)));
        var number = new FlowHarness(If("3", 10, ValueOf("'x'", null, 11)));

        await Assert.That(Run(emptyString)).IsEqualTo(string.Empty);
        await Assert.That(Run(emptySequence)).IsEqualTo(string.Empty);
        await Assert.That(Run(number)).IsEqualTo("x");
    }

    [Test]
    public async Task If_FailingTest_ReturnsTheProblem_AndRunsNoBody()
    {
        var harness = new FlowHarness(If("1 idiv 0", 10, ValueOf("'yes'", null, 11)));

        await Assert.That(Run(harness)).IsEqualTo("problem");
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task If_LocalVariableOfTheBody_IsVisibleToTheLaterSiblingsInTheBody()
    {
        var harness = new FlowHarness(If("true()", 10, Var("v", "5", 11), ValueOf("$v + 1", null, 12)));

        await Assert.That(Run(harness)).IsEqualTo("6");
    }

    [Test]
    public async Task If_LocalVariableOfTheBody_IsNotVisibleAfterTheInstruction()
    {
        var harness = new FlowHarness(
            ValueOf(null, null, 9, If("true()", 10, Var("v", "5", 11)), ValueOf("$v", null, 12))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3300/XPST0008");
    }

    [Test]
    public async Task If_WithoutTest_IsStaticErrorMissingAttribute()
    {
        var harness = new FlowHarness(If(null, 10, ValueOf("'x'", null, 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3005");
    }

    [Test]
    public async Task If_ReportsTheSlotsOfTheTestAndOfTheBody()
    {
        var harness = new FlowHarness(If("$a = 1", 10, ValueOf("$c", null, 11)), "a", "b", "c");

        await Assert.That(((IXsltSlotReader)harness.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([0, 2]);
    }

    [Test]
    public async Task Choose_FirstTrueWhenWins_AndLaterWhensAreNotEvaluated()
    {
        var harness = new FlowHarness(
            Choose(
                10,
                When("1 = 2", 11, ValueOf("'a'", null, 12)),
                When("2 = 2", 13, ValueOf("'b'", null, 14)),
                When("1 idiv 0", 15, ValueOf("'c'", null, 16)),
                Otherwise(17, ValueOf("'o'", null, 18))
            )
        );

        await Assert.That(Run(harness)).IsEqualTo("b");
    }

    [Test]
    public async Task Choose_NoWhenIsTrue_FallsThroughToOtherwise()
    {
        var harness = new FlowHarness(
            Choose(
                10,
                When("1 = 2", 11, ValueOf("'a'", null, 12)),
                When("2 = 3", 13, ValueOf("'b'", null, 14)),
                Otherwise(15, ValueOf("'o'", null, 16))
            )
        );

        await Assert.That(Run(harness)).IsEqualTo("o");
    }

    [Test]
    public async Task Choose_NoWhenIsTrueAndNoOtherwise_ProducesNothing()
    {
        var harness = new FlowHarness(Choose(10, When("1 = 2", 11, ValueOf("'a'", null, 12))));

        await Assert.That(Run(harness)).IsEqualTo(string.Empty);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Choose_FailingWhenTest_ReturnsTheProblem()
    {
        var harness = new FlowHarness(
            Choose(10, When("1 idiv 0", 11, ValueOf("'a'", null, 12)), Otherwise(13, ValueOf("'o'", null, 14)))
        );

        await Assert.That(Run(harness)).IsEqualTo("problem");
    }

    [Test]
    public async Task Choose_BranchLocalVariables_StayInTheirBranch()
    {
        var harness = new FlowHarness(
            Choose(
                10,
                When("false()", 11, Var("v", "1", 12), ValueOf("$v", null, 13)),
                Otherwise(14, Var("v", "2", 15), ValueOf("$v", null, 16))
            )
        );

        await Assert.That(Run(harness)).IsEqualTo("2");
    }

    [Test]
    public async Task Choose_ReportsTheSlotsOfEveryTestAndBranch()
    {
        var harness = new FlowHarness(
            Choose(
                10,
                When("$a = 1", 11, ValueOf("$b", null, 12)),
                Otherwise(13, ValueOf("$c", null, 14))
            ),
            "a",
            "b",
            "c"
        );

        await Assert.That(((IXsltSlotReader)harness.Executable!).ReferencedSlots.ToArray()).IsEquivalentTo([0, 1, 2]);
    }

    [Test]
    public async Task Choose_WithoutWhen_IsStaticErrorXtse0010()
    {
        var harness = new FlowHarness(Choose(10, Otherwise(11, ValueOf("'o'", null, 12))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task Choose_OtherwiseBeforeAWhen_IsStaticErrorXtse0010()
    {
        var harness = new FlowHarness(
            Choose(10, When("true()", 11), Otherwise(12), When("true()", 13))
        );

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task Choose_TwoOtherwise_IsStaticErrorXtse0010()
    {
        var harness = new FlowHarness(Choose(10, When("true()", 11), Otherwise(12), Otherwise(13)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task Choose_WhenWithoutTest_IsStaticErrorMissingAttribute()
    {
        var harness = new FlowHarness(Choose(10, When(null, 11)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3005");
    }

    [Test]
    public async Task Choose_OtherChildElement_IsStaticErrorXtse0010()
    {
        var harness = new FlowHarness(Choose(10, When("true()", 11), Seq("1", 12)));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }

    [Test]
    public async Task WhenAndOtherwise_OutsideChoose_AreMisplaced()
    {
        var when = new FlowHarness(When("true()", 10));
        var otherwise = new FlowHarness(Otherwise(10));

        await Assert.That(when.CompileCodes).IsEqualTo("NES3003/XTSE0010");
        await Assert.That(otherwise.CompileCodes).IsEqualTo("NES3003/XTSE0010");
    }
}
