namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals.GlobalFixture;

/// <summary>
/// The scopes of a schema with global <c>xsl:variable</c> and <c>xsl:param</c> declarations (design #150 EP-4): they take slots of
/// the schema level next to the parameters and the lets, are order independent, are visible in every scope below the schema level
/// and in the scope of functions, and share the namespace of names with the schema level.
/// </summary>
public class GlobalPlanTests
{
    private static VariableSlot Find(ScopePlan plan, string name)
    {
        foreach (var slot in plan.Globals)
        {
            if (string.Equals(slot.Name, name, StringComparison.Ordinal))
            {
                return slot;
            }
        }

        throw new InvalidOperationException("No slot " + name);
    }

    private static bool Reads(VariableSlot reader, VariableSlot read)
    {
        foreach (var slot in reader.Value!.ReferencedSlots)
        {
            if (slot == read.Slot)
            {
                return true;
            }
        }

        return false;
    }

    [Test]
    public async Task Plan_Globals_TakeTheSlotsAfterTheParametersAndBeforeTheLets()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "g1"), A("select", "1")),
            X("param", 11, A("name", "g2"), A("select", "2"))
        );
        var schema = Schema(lets: [Let("a", "1")], parameters: [Param("p", "'d'")]);

        var ok = fixture.TryPlan(schema, table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(ScopeFixture.Visible(plan.GlobalScope)).IsEqualTo("p,g1,g2,a");
        await Assert.That(Find(plan, "p").Slot).IsEqualTo(0);
        await Assert.That(Find(plan, "g1").Slot).IsEqualTo(1);
        await Assert.That(Find(plan, "g2").Slot).IsEqualTo(2);
        await Assert.That(Find(plan, "a").Slot).IsEqualTo(3);
        await Assert.That(plan.PatternStart).IsEqualTo(4);
        await Assert.That(plan.RuleStart).IsEqualTo(4);
        await Assert.That(plan.SlotCount).IsEqualTo(4);
    }

    [Test]
    public async Task Plan_AGlobalVariableAndAGlobalParam_AreSlotsOfTheSchemaLevelWithTheirValueAndType()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "g"), A("select", "1 + 1"), A("as", "xs:integer")),
            X("param", 11, A("name", "p"), A("select", "'d'"))
        );

        _ = fixture.TryPlan(EmptySchema(), table, out var plan);

        var variable = Find(plan, "g");
        await Assert.That(variable.Level).IsEqualTo(ScopeLevel.Schema);
        await Assert.That(variable.IsParameter).IsFalse();
        await Assert.That(variable.DeclaredType).IsEqualTo("xs:integer");
        await Assert.That(variable.Value).IsNotNull();
        await Assert.That(variable.Location.Line).IsEqualTo(10);
        await Assert.That(variable.StaticProblem).IsNull();
        var param = Find(plan, "p");
        await Assert.That(param.IsParameter).IsTrue();
        await Assert.That(param.Value).IsNotNull();
    }

    [Test]
    public async Task Plan_ARequiredParam_HasNoValueUntilTheCallerGivesOne()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("param", 10, A("name", "p"), A("required", "yes")));

        var ok = fixture.TryPlan(EmptySchema(), table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(Find(plan, "p").Value).IsNull();
        await Assert.That(Find(plan, "p").IsParameter).IsTrue();
    }

    [Test]
    public async Task Plan_AGlobalMayReadAGlobalThatIsDeclaredLater()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "a"), A("select", "$b")),
            X("variable", 11, A("name", "b"), A("select", "1"))
        );

        var ok = fixture.TryPlan(EmptySchema(), table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(Reads(Find(plan, "a"), Find(plan, "b"))).IsTrue();
    }

    [Test]
    public async Task Plan_ASchemaLetMayReadAGlobalThatIsDeclaredLater_AndAGlobalMayReadAnyLet()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "g"), A("select", "$a")));
        var schema = Schema(lets: [Let("a", "1"), Let("b", "$g")]);

        var ok = fixture.TryPlan(schema, table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(Reads(Find(plan, "g"), Find(plan, "a"))).IsTrue();
        await Assert.That(Reads(Find(plan, "b"), Find(plan, "g"))).IsTrue();
    }

    [Test]
    public async Task Plan_AGlobalMayReadASchemaParam()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "g"), A("select", "$p")));

        var ok = fixture.TryPlan(Schema(parameters: [Param("p", "'d'")]), table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(Reads(Find(plan, "g"), Find(plan, "p"))).IsTrue();
    }

    [Test]
    public async Task Plan_AGlobalThatReadsItself_Compiles_BecauseTheCycleIsFoundWhenItIsUsed()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "g"), A("select", "$g + 1")));

        var ok = fixture.TryPlan(EmptySchema(), table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(Reads(Find(plan, "g"), Find(plan, "g"))).IsTrue();
    }

    [Test]
    public async Task Plan_AGlobalWithTheNameOfASchemaLet_IsNes3200AtTheLet()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "x"), A("select", "1")));
        var schema = Schema(lets: [Let("x", "2")]);

        var ok = fixture.TryPlan(schema, table, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200");
    }

    [Test]
    public async Task Plan_AGlobalWithTheNameOfASchemaParam_IsNes3200AtTheGlobal()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "x"), A("select", "1")));
        var schema = Schema(parameters: [Param("x", "'d'")]);

        var ok = fixture.TryPlan(schema, table, out var plan);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(ScopeFixture.Visible(plan.GlobalScope)).IsEqualTo("x");
    }

    [Test]
    public async Task Plan_TwoGlobalsOfOneNameThatReachThePlannerTogether_AreNes3200()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "x"), A("select", "1")));
        var twice = new List<GlobalDeclaration> { table.Declarations[0], table.Declarations[0] };
        _ = PhaseSelector.TryResolve(EmptySchema(), "#ALL", fixture.Problems, out var selection);

        var ok = ScopePlanner.TryPlan(EmptySchema(), selection, twice, fixture.Binding, fixture.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200");
    }

    [Test]
    public async Task Plan_AGlobalCannotReadAPhaseLet_BecauseThePhaseIsChosenPerRun()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "g"), A("select", "$x")));
        var schema = Schema(phases: [Phase("ph", [Let("x", "1")], "p")], patterns: Pattern("p", [], Rule("r", "a")));

        var ok = fixture.TryPlan(schema, table, out _, "ph");

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
    }

    [Test]
    public async Task Plan_AGlobalCannotReadAPatternLet_BecauseItBelongsToItsPattern()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("variable", 10, A("name", "g"), A("select", "$pl")));
        var schema = Schema(patterns: Pattern("p", [Let("pl", "1")], Rule("r", "a")));

        var ok = fixture.TryPlan(schema, table, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
    }

    [Test]
    public async Task Plan_PatternsRulesAndFunctions_SeeTheGlobals()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "g"), A("select", "1")),
            X("param", 11, A("name", "p"), A("select", "2"))
        );
        var schema = Schema(patterns: Pattern("p1", [Let("pl", "$g")], Rule("r", "a", Let("rl", "$p + $pl"))));

        var ok = fixture.TryPlan(schema, table, out var plan);

        await Assert.That(ok).IsTrue();
        await Assert.That(ScopeFixture.Visible(plan.GlobalScope)).IsEqualTo("g,p");
        await Assert.That(ScopeFixture.Visible(plan.Patterns[0].Scope)).IsEqualTo("g,p,pl");
        await Assert.That(ScopeFixture.Visible(plan.Patterns[0].Rules[0].Scope)).IsEqualTo("g,p,pl,rl");
        await Assert.That(plan.PatternStart).IsEqualTo(2);
    }

    [Test]
    public async Task Plan_AGlobalThatDoesNotCompile_IsReportedOnce_AndItsNameStaysVisibleToTheReaders()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(
            0,
            X("variable", 10, A("name", "bad"), A("select", "$nowhere")),
            X("variable", 11, A("name", "ok"), A("select", "$bad"))
        );

        var ok = fixture.TryPlan(EmptySchema(), table, out var plan);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3300/XPST0008");
        await Assert.That(ScopeFixture.Visible(plan.GlobalScope)).IsEqualTo("bad,ok");
        await Assert.That(plan.Globals.Count).IsEqualTo(1);
        await Assert.That(plan.Globals[0].Name).IsEqualTo("ok");
    }

    [Test]
    public async Task Plan_WithoutGlobals_GivesTheSameSlotsAsThePlannerWithoutThem()
    {
        var fixture = new GlobalFixture();
        var schema = Schema(lets: [Let("a", "1")], parameters: [Param("p", "'d'")]);
        _ = PhaseSelector.TryResolve(schema, "#ALL", fixture.Problems, out var selection);

        var with = fixture.TryPlan(schema, fixture.Table(0), out var planWith);
        var without = ScopePlanner.TryPlan(schema, selection, fixture.Binding, fixture.Problems, out var planWithout);

        await Assert.That(with).IsTrue();
        await Assert.That(without).IsTrue();
        await Assert
            .That(ScopeFixture.Visible(planWith.GlobalScope))
            .IsEqualTo(ScopeFixture.Visible(planWithout.GlobalScope));
        await Assert.That(planWith.SlotCount).IsEqualTo(planWithout.SlotCount);
    }

    [Test]
    public async Task Guards_RejectNullArguments()
    {
        var fixture = new GlobalFixture();
        _ = PhaseSelector.TryResolve(EmptySchema(), "#ALL", fixture.Problems, out var selection);

        await Assert
            .That(() => ScopePlanner.TryPlan(EmptySchema(), selection, null!, fixture.Binding, fixture.Problems, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PlanRun(EmptySchema(), fixture.Binding, fixture.Problems, null!))
            .Throws<ArgumentNullException>();
    }
}
