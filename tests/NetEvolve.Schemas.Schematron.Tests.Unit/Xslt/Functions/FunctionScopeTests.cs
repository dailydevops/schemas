namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// What a function body sees (design #150 EP-4, EP-5, section 13): the schema-level <c>let</c> and <c>param</c>, bound lazily once
/// per run through the shared slot binder when the function is entered, and nothing of a phase, a pattern, or a rule. A global
/// pattern <c>let</c> in the wording of issue 249 is a schema-level <c>let</c> (design #150 section 13).
/// </summary>
public class FunctionScopeTests
{
    private static FunctionFixture Planned() =>
        ForSchema(
            Schema(
                lets: [Let("base", "10"), Let("scaled", "$base * 2")],
                parameters: [Param("factor", "3")],
                phases: [Phase("ph", [Let("phaseLet", "1")], "p")],
                patterns: Pattern("p", [Let("patternLet", "2")], Rule("r", "a", Let("ruleLet", "3")))
            )
        );

    [Test]
    public async Task Function_ReadsASchemaLet_WhichIsBoundOnceAtTheFirstCall()
    {
        var fixture = Planned();
        _ = fixture.CompileAll(Simple("plus-base", "xs:integer", ["x"], "$x + $base", 10));
        var (run, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "plus-base"), 1, out var function);

        var first = caller.TryCall(function, [Int(5)], out var result);
        var second = caller.TryCall(function, [Int(7)], out var again);

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Integer(result)).IsEqualTo(15);
        await Assert.That(second).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Integer(again)).IsEqualTo(17);
        var baseSlot = fixture.Plan!.Globals[1].Slot;
        await Assert.That(run.Events.FindAll(e => e == "bind:" + baseSlot).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Function_ReadsALetThatReadsAnotherLet_AndAParam()
    {
        var fixture = Planned();
        _ = fixture.CompileAll(Simple("both", "xs:integer", ["x"], "$x + $scaled + xs:integer($factor)", 10));
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "both"), 1, out var function);

        var status = caller.TryCall(function, [Int(1)], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Integer(result)).IsEqualTo(24);
    }

    [Test]
    public async Task Function_RecordsTheSchemaSlotsItsBodyReads_ForTheBinderAtEntry()
    {
        var fixture = Planned();

        var component = fixture.CompileOne(Simple("plus-base", "xs:integer", ["x"], "$x + $base", 10));

        var baseSlot = fixture.Plan!.Globals[1].Slot;
        await Assert.That(component!.ReferencedSlots.ToArray()).IsEquivalentTo([baseSlot]);
    }

    [Test]
    [Arguments("$phaseLet")]
    [Arguments("$patternLet")]
    [Arguments("$ruleLet")]
    public async Task Function_ReadingAPhasePatternOrRuleLet_FailsAtBuildWithVariableUndefined(string select)
    {
        var fixture = Planned();

        var component = fixture.CompileOne(Simple("bad", "xs:integer", ["x"], select, 10));

        await Assert.That(component).IsNull();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
    }

    [Test]
    public async Task Function_Slots_LieAboveEverySlotOfTheSchemaPhasePatternAndRuleBlocks()
    {
        var fixture = Planned();

        var component = fixture.CompileOne(Simple("any", "xs:integer", ["x"], "$x", 10));

        await Assert.That(component!.SlotBase).IsEqualTo(fixture.Plan!.SlotCount);
        await Assert.That(component.Parameters[0].Slot).IsEqualTo(fixture.Plan.SlotCount);
    }

    [Test]
    public async Task Function_ReadingAFailedLet_ReportsTheProblemOnceAndFailsTheCall()
    {
        var fixture = ForSchema(Schema(lets: [Let("bad", "1 idiv 0")]));
        _ = fixture.CompileAll(Simple("read", "xs:integer", ["x"], "$x + $bad", 10));
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(Functions, "read"), 1, out var function);

        var first = caller.TryCall(function, [Int(1)], out _);
        var second = caller.TryCall(function, [Int(1)], out _);

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(caller.Depth).IsEqualTo(0);
    }
}
