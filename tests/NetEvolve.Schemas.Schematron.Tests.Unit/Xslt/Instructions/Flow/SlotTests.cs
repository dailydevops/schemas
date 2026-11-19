namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow.Fl;

/// <summary>
/// The variable slots of the bodies that the flow instructions nest: a function saves and restores every slot of its body, so a
/// local variable inside <c>xsl:choose</c>, <c>xsl:if</c>, or <c>xsl:for-each</c> survives a recursive call.
/// </summary>
public class SlotTests
{
    private static async Task<long> CallSum(FunctionFixture fixture, long n)
    {
        var (_, caller) = fixture.CreateRun();
        _ = fixture.Table.TryGet(new XPathQName(FunctionTree.Functions, "sum"), 1, out var function);

        var status = caller.TryCall(function, [Int(n)], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        return MiniQuery.Integer(result);
    }

    [Test]
    public async Task Recursion_LocalVariablesInAnOtherwiseBranch_KeepTheValuesOfEveryActivation()
    {
        var fixture = Create();
        var component = fixture.CompileOne(
            Fx.Function(
                "f:sum",
                "xs:integer",
                10,
                Fx.Param("n", "xs:integer", 11),
                Choose(
                    12,
                    When("$n = 0", 13, Fx.Sequence("0", 14)),
                    Otherwise(15, Var("v", "$n", 16), Var("rest", "f:sum($n - 1)", 17), Fx.Sequence("$v + $rest", 18))
                )
            )
        );

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(component!.SlotCount).IsEqualTo(3);
        await Assert.That(await CallSum(fixture, 4)).IsEqualTo(10);
    }

    [Test]
    public async Task Recursion_LocalVariablesInAnIfBody_KeepTheValuesOfEveryActivation()
    {
        var fixture = Create();
        var component = fixture.CompileOne(
            Fx.Function(
                "f:sum",
                "xs:integer",
                10,
                Fx.Param("n", "xs:integer", 11),
                If("$n != 0", 12, Var("v", "$n", 13), Var("rest", "f:sum($n - 1)", 14), Fx.Sequence("$v + $rest", 15)),
                If("$n = 0", 16, Fx.Sequence("0", 17))
            )
        );

        await Assert.That(component!.SlotCount).IsEqualTo(3);
        await Assert.That(await CallSum(fixture, 5)).IsEqualTo(15);
    }

    [Test]
    public async Task Recursion_LocalVariablesInAForEachBody_KeepTheValuesOfEveryActivation()
    {
        var fixture = Create();
        var component = fixture.CompileOne(
            Fx.Function(
                "f:sum",
                "xs:integer",
                10,
                Fx.Param("n", "xs:integer", 11),
                If(
                    "$n = 0",
                    12,
                    Fx.Sequence("0", 13)
                ),
                If(
                    "$n != 0",
                    14,
                    ForEach("$n", 15, Var("v", ".", 16), Var("rest", "f:sum($n - 1)", 17), Fx.Sequence("$v + $rest", 18))
                )
            )
        );

        await Assert.That(component!.SlotCount).IsEqualTo(3);
        await Assert.That(await CallSum(fixture, 5)).IsEqualTo(15);
    }

    [Test]
    public async Task Body_VariablesAfterANestedInstruction_DoNotShareASlotWithItsLocals()
    {
        var fixture = Create();
        var component = fixture.CompileOne(
            Fx.Function(
                "f:sum",
                "xs:integer",
                10,
                Fx.Param("n", "xs:integer", 11),
                If("true()", 12, Var("inner", "1", 13)),
                Var("outer", "2", 14),
                Fx.Sequence("$outer", 15)
            )
        );

        await Assert.That(component!.SlotCount).IsEqualTo(3);
        await Assert.That(await CallSum(fixture, 1)).IsEqualTo(2);
    }
}
