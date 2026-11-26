namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;

/// <summary>
/// The opt-in global visibility of a pattern-level <see langword="let"/> (#615, design #150 EP-4 option B): the pattern lets of the active
/// patterns move into the schema-level block, a collision with a schema-level name is <c>SCH-LET-DUPLICATE</c>, and a phase or rule
/// <see langword="let"/> still shadows. With the switch off nothing changes.
/// </summary>
public class GlobalPatternLetTests
{
    private static SchematronSchema TwoPatterns() =>
        Schema(patterns: [Pattern("p1", [Let("g", "1")], Rule("r1", "/")), Pattern("p2", [], Rule("r2", "/"))]);

    [Test]
    public async Task Hoisted_PatternLet_IsVisibleInALaterPatternItsRulesAndTheFunctionScope()
    {
        var f = Create(TwoPatterns(), hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("g");
        await Assert.That(Visible(f.Plan.Patterns[1].Scope)).IsEqualTo("g");
        await Assert.That(Visible(f.Plan.Patterns[1].Rules[0].Scope)).IsEqualTo("g");
        await Assert.That(f.Plan.Patterns[0].Lets.Count).IsEqualTo(0);
        await Assert.That(f.Plan.Globals.Count).IsEqualTo(1);
        await Assert.That(f.Plan.Globals[0].Level).IsEqualTo(ScopeLevel.Schema);
        await Assert.That(f.Plan.PatternStart).IsEqualTo(1);
        await Assert.That(f.Plan.RuleStart).IsEqualTo(1);
    }

    [Test]
    public async Task Hoisted_PatternLet_IsVisibleInAnEarlierPatternByName()
    {
        var schema = Schema(
            patterns: [Pattern("p1", [], Rule("r1", "/")), Pattern("p2", [Let("late", "1")], Rule("r2", "/"))]
        );

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.Patterns[0].Scope)).IsEqualTo("late");
    }

    [Test]
    public async Task Hoisted_PatternLets_FollowTheSchemaLetsInDocumentOrder_AndMayReadEarlierOnes()
    {
        var schema = Schema(
            lets: [Let("a", "1")],
            patterns:
            [
                Pattern("p1", [Let("b", "$a + 1")], Rule("r1", "/")),
                Group("g1", [Let("c", "$b + 1")], Rule("r2", "/")),
            ]
        );

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("a,b,c");
        await Assert.That(f.Plan.Globals[1].Slot).IsEqualTo(1);
        await Assert.That(f.Plan.Globals[2].Slot).IsEqualTo(2);
        await Assert.That(f.Plan.Globals[2].Value!.ReferencedSlots.ToArray()).IsEquivalentTo([1]);
    }

    [Test]
    public async Task Hoisted_PatternLet_ReadingALaterOne_IsVariableUndefined()
    {
        var schema = Schema(
            patterns:
            [
                Pattern("p1", [Let("x", "$y")], Rule("r1", "/")),
                Pattern("p2", [Let("y", "1")], Rule("r2", "/")),
            ]
        );

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3300");
    }

    [Test]
    public async Task Hoisted_PatternLet_WithTheNameOfASchemaLet_IsLetDuplicate()
    {
        var schema = Schema(lets: [Let("x", "0")], patterns: Pattern("p", [Let("x", "1")], Rule("r", "/")));

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3200");
        await Assert.That(f.Plan.Globals.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Hoisted_PatternLet_WithTheNameOfASchemaParam_IsLetDuplicate()
    {
        var schema = Schema(parameters: [Param("x", "0")], patterns: Pattern("p", [Let("x", "1")], Rule("r", "/")));

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3200");
    }

    [Test]
    public async Task Hoisted_PatternLet_WithTheNameOfAnotherPatternsLet_IsLetDuplicate()
    {
        var second = Let("x", "2");
        var schema = Schema(
            patterns: [Pattern("p1", [Let("x", "1")], Rule("r1", "/")), Group("g1", [second], Rule("r2", "/"))]
        );

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3200");
        await Assert.That(f.Problems.Problems[0].Location.Line).IsEqualTo(second.Location.Line);
    }

    [Test]
    public async Task Hoisted_PatternLet_WithTheNameOfAGlobalVariable_IsLetDuplicate()
    {
        var schema = Schema(patterns: Pattern("p", [Let("x", "1")], Rule("r", "/")));
        var problems = new FakeProblemSink();
        _ = PhaseSelector.TryResolve(schema, "#ALL", problems, out var selection);
        GlobalDeclaration[] globals = [new("x", "0", null, false, false, Next())];

        var ok = ScopePlanner.TryPlan(
            schema,
            selection,
            globals,
            new ScopeTestBinding(),
            problems,
            out _,
            hoistPatternLets: true
        );

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(problems)).IsEqualTo("NES3200");
    }

    [Test]
    public async Task Hoisted_PatternLet_WithAnAsTypeThatTheConstantViolates_IsReportedAtBuild()
    {
        var schema = Schema(patterns: Pattern("p", [Let("n", "'a'", "xs:integer")], Rule("r", "/")));

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3201");
    }

    [Test]
    public async Task PhaseLet_ShadowsAHoistedPatternLet_WithoutAnError()
    {
        var schema = Schema(
            phases: [Phase("ph", [Let("x", "2")], "p")],
            patterns: Pattern("p", [Let("x", "1")], Rule("r", "/"))
        );

        var f = Create(schema, "ph", hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(SlotOf(f.Plan.GlobalScope, "x")).IsEqualTo(0);
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "x")).IsEqualTo(1);
        await Assert.That(f.Plan.PatternStart).IsEqualTo(2);
    }

    [Test]
    public async Task RuleLet_ShadowsAHoistedPatternLet_WithoutAnError()
    {
        var schema = Schema(patterns: Pattern("p", [Let("x", "1")], Rule("r", "a", Let("x", "2"))));

        var f = Create(schema, hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(SlotOf(f.Plan.Patterns[0].Rules[0].Scope, "x")).IsEqualTo(f.Plan.RuleStart);
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "x")).IsEqualTo(0);
    }

    [Test]
    public async Task Hoisting_OnlyCoversThePatternsOfTheSelectedPhase()
    {
        var schema = Schema(
            phases: [Phase("ph", [], "p1")],
            patterns: [Pattern("p1", [Let("a", "1")], Rule("r1", "/")), Pattern("p2", [Let("b", "2")], Rule("r2", "/"))]
        );

        var f = Create(schema, "ph", hoistPatternLets: true);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("a");
    }

    [Test]
    public async Task Off_KeepsAPatternLetLexical_AndShadowingASchemaLetIsNoError()
    {
        var schema = Schema(
            lets: [Let("x", "0")],
            patterns:
            [
                Pattern("p1", [Let("x", "1"), Let("g", "2")], Rule("r1", "/")),
                Pattern("p2", [], Rule("r2", "/")),
            ]
        );

        var f = Create(schema, hoistPatternLets: false);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("x");
        await Assert.That(Visible(f.Plan.Patterns[1].Scope)).IsEqualTo("x");
        await Assert.That(f.Plan.Patterns[0].Lets.Count).IsEqualTo(2);
        await Assert.That(f.Plan.PatternStart).IsEqualTo(1);
        await Assert.That(f.Plan.RuleStart).IsEqualTo(3);
    }
}
