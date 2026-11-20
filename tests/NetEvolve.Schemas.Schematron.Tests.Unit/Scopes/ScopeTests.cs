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
/// The static scope model of design #150 EP-4: schema, phase, pattern, and rule levels, inner shadows outer (#106 Q2),
/// duplicates at one level, and the function scope, which sees only the schema level.
/// </summary>
public class ScopeTests
{
    private static readonly int[] FirstSlot = [0];

    private static SchematronProblem Single(ScopeFixture f)
    {
        if (f.Problems.Problems.Count != 1)
        {
            throw new InvalidOperationException($"Expected one problem, got [{Codes(f.Problems)}].");
        }

        return f.Problems.Problems[0];
    }

    [Test]
    public async Task Variable_IsVisibleInItsOwnScopeAndAllInnerScopes()
    {
        var schema = Schema(
            lets: [Let("a", "1")],
            phases: [Phase("ph", [Let("b", "2")], "p")],
            patterns: Pattern("p", [Let("c", "3")], Rule("r", "x", Let("d", "4")))
        );

        var f = Create(schema, "ph");

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("a");
        await Assert.That(Visible(f.Plan.Patterns[0].Scope)).IsEqualTo("a,b,c");
        await Assert.That(Visible(f.Plan.Patterns[0].Rules[0].Scope)).IsEqualTo("a,b,c,d");
    }

    [Test]
    public async Task Variable_IsNotVisibleOutsideItsScope()
    {
        var schema = Schema(
            patterns:
            [
                Pattern("p1", [Let("c", "1")], Rule("r1", "x", Let("d", "2")), Rule("r2", "y")),
                Pattern("p2", [], Rule("r3", "z")),
            ]
        );

        var f = Create(schema);

        await Assert.That(Visible(f.Plan.Patterns[0].Scope)).IsEqualTo("c");
        await Assert.That(Visible(f.Plan.Patterns[0].Rules[0].Scope)).IsEqualTo("c,d");
        await Assert.That(Visible(f.Plan.Patterns[0].Rules[1].Scope)).IsEqualTo("c");
        await Assert.That(Visible(f.Plan.Patterns[1].Scope)).IsEqualTo(string.Empty);
        await Assert.That(Visible(f.Plan.Patterns[1].Rules[0].Scope)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task PatternLet_IsNotVisibleInLaterPattern_ThrowsVariableUndefined()
    {
        var schema = Schema(
            patterns: [Pattern("p1", [Let("foobar", "1")], Rule("r1", "/")), Pattern("p2", [], Rule("r2", "/"))]
        );
        var f = Create(schema);

        var found = f.Binding.TryCompile(
            "$foobar = 1",
            QueryExpressionKind.Boolean,
            f.Plan.Patterns[1].Rules[0].Scope,
            out _,
            out var problem
        );

        await Assert.That(found).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }

    [Test]
    public async Task RuleLet_ShadowsPatternLet()
    {
        var schema = Schema(patterns: Pattern("p", [Let("x", "1")], Rule("r1", "a", Let("x", "2"))));

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(SlotOf(f.Plan.Patterns[0].Rules[0].Scope, "x")).IsEqualTo(f.Plan.RuleStart);
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "x")).IsEqualTo(f.Plan.PatternStart);
    }

    [Test]
    public async Task PatternLet_ShadowsSchemaLet()
    {
        var schema = Schema(lets: [Let("x", "0")], patterns: Pattern("p", [Let("x", "1")], Rule("r", "a")));

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "x")).IsEqualTo(f.Plan.PatternStart);
        await Assert.That(SlotOf(f.Plan.GlobalScope, "x")).IsEqualTo(0);
    }

    [Test]
    public async Task PhaseLet_ShadowsSchemaLet()
    {
        var schema = Schema(
            lets: [Let("foo", "0")],
            phases: [Phase("ph", [Let("foo", "1")], "p")],
            patterns: Pattern("p", [], Rule("r", "/"))
        );

        var f = Create(schema, "ph");

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "foo")).IsEqualTo(1);
        await Assert.That(SlotOf(f.Plan.GlobalScope, "foo")).IsEqualTo(0);
    }

    [Test]
    public async Task PhaseLet_NotInAllPhase()
    {
        var schema = Schema(
            lets: [Let("foo", "0")],
            phases: [Phase("ph", [Let("foo", "1")], "p")],
            patterns: Pattern("p", [], Rule("r", "/"))
        );

        var f = Create(schema);

        await Assert.That(f.Plan.PhaseLets.Count).IsEqualTo(0);
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "foo")).IsEqualTo(0);
    }

    [Test]
    public async Task Let_ForwardReference_ThrowsVariableUndefined()
    {
        var schema = Schema(patterns: Pattern("p", [], Rule("r", "a", Let("y", "$x"), Let("x", "1"))));

        var f = Create(schema);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Single(f).Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }

    [Test]
    public async Task Let_SelfReference_ThrowsVariableUndefined()
    {
        var schema = Schema(lets: [Let("x", "$x + 1")]);

        var f = Create(schema);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Single(f).Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }

    [Test]
    public async Task Let_ReadingAnEarlierSibling_IsCompiledAgainstItsSlot()
    {
        var schema = Schema(lets: [Let("a", "1"), Let("b", "$a")]);

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Plan.Globals[1].Value!.ReferencedSlots.ToArray()).IsEquivalentTo(FirstSlot);
    }

    [Test]
    public async Task RuleContext_UsesPatternPhaseAndSchemaVariables()
    {
        var schema = Schema(
            lets: [Let("s", "1")],
            phases: [Phase("ph", [Let("f", "2")], "p")],
            patterns: Pattern("p", [Let("n", "'a'")], Rule("r1", "*[local-name() = $n]", Let("r", "3")))
        );

        var f = Create(schema, "ph");

        await Assert.That(Visible(f.Plan.Patterns[0].Scope)).IsEqualTo("s,f,n");
        await Assert.That(SlotOf(f.Plan.Patterns[0].Scope, "r")).IsEqualTo(-1);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task Let_DuplicateAtOneLevel_ThrowsLetDuplicate(int level)
    {
        var first = Let("x", "1");
        var second = Let("x", "2");
        var schema = level switch
        {
            0 => Schema(lets: [first, second]),
            1 => Schema(phases: [Phase("ph", [first, second], "p")], patterns: Pattern("p", [], Rule("r", "a"))),
            2 => Schema(patterns: Pattern("p", [first, second], Rule("r", "a"))),
            _ => Schema(patterns: Pattern("p", [], Rule("r", "a", first, second))),
        };

        var f = Create(schema, level == 1 ? "ph" : "#ALL");

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Single(f).Code).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate);
        await Assert.That(Single(f).Location).IsEqualTo(second.Location);
    }

    [Test]
    public async Task Param_And_Let_SameNameAtSchemaLevel_ThrowsLetDuplicate()
    {
        var let = Let("x", "1");
        var schema = Schema(lets: [let], parameters: [Param("x", "'a'")]);

        var f = Create(schema);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Single(f).Code).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate);
        await Assert.That(Single(f).Location).IsEqualTo(let.Location);
    }

    [Test]
    public async Task Param_SameNameTwice_ThrowsLetDuplicate()
    {
        var second = Param("x", "'b'");
        var schema = Schema(parameters: [Param("x", "'a'"), second]);

        var f = Create(schema);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Single(f).Code).IsEqualTo(SchematronDiagnosticCodes.LetDuplicate);
        await Assert.That(Single(f).Location).IsEqualTo(second.Location);
    }

    [Test]
    public async Task Param_IsVisibleToLetsPatternsAndFunctions()
    {
        var schema = Schema(
            lets: [Let("a", "$p")],
            parameters: [Param("p", "'x'")],
            patterns: Pattern("pt", [], Rule("r", "a"))
        );

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(Visible(f.Plan.GlobalScope)).IsEqualTo("p,a");
        await Assert.That(Visible(f.Plan.Patterns[0].Rules[0].Scope)).IsEqualTo("p,a");
    }

    [Test]
    public async Task Group_HasTheSameScopesAsPattern()
    {
        var schema = Schema(
            edition: SchematronEdition.Iso2025,
            patterns: Group("g", [Let("x", "1")], Rule("r", "a", Let("y", "2")))
        );

        var f = Create(schema);

        await Assert.That(Visible(f.Plan.Patterns[0].Rules[0].Scope)).IsEqualTo("x,y");
    }

    [Test]
    public async Task FunctionScope_FindsSchemaLetAndParam_ButNotPhaseOrPatternLet()
    {
        var schema = Schema(
            lets: [Let("sl", "1")],
            parameters: [Param("sp", "'a'")],
            phases: [Phase("ph", [Let("pl", "2")], "p")],
            patterns: Pattern("p", [Let("tl", "3")], Rule("r", "a"))
        );
        var f = Create(schema, "ph");

        var schemaLet = f.Binding.TryCompile("$sl, $sp", QueryExpressionKind.Value, f.Plan.GlobalScope, out _, out _);
        var phaseLet = f.Binding.TryCompile("$pl", QueryExpressionKind.Value, f.Plan.GlobalScope, out _, out var phase);
        var patternLet = f.Binding.TryCompile(
            "$tl",
            QueryExpressionKind.Value,
            f.Plan.GlobalScope,
            out _,
            out var pattern
        );

        await Assert.That(schemaLet).IsTrue();
        await Assert.That(phaseLet).IsFalse();
        await Assert.That(phase.Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
        await Assert.That(patternLet).IsFalse();
        await Assert.That(pattern.Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }

    [Test]
    public async Task Namespaces_FromNsElements_AreInEveryScope()
    {
        var schema = Schema(
            lets: [Let("a", "1")],
            namespaces: [Ns("p", "urn:p"), Ns("q", "urn:q")],
            patterns: Pattern("pt", [Let("b", "2")], Rule("r", "a", Let("c", "3")))
        );

        var f = Create(schema);

        foreach (
            var scope in new[]
            {
                f.Plan.GlobalScope,
                f.Plan.Patterns[0].Scope,
                f.Plan.Patterns[0].Rules[0].Scope,
                f.Binding.Compiled[0].Scope,
                f.Binding.Compiled[2].Scope,
            }
        )
        {
            await Assert.That(scope.Namespaces.Count).IsEqualTo(2);
            await Assert.That(scope.Namespaces[0].Prefix).IsEqualTo("p");
            await Assert.That(scope.Namespaces[1].NamespaceName).IsEqualTo("urn:q");
        }
    }

    [Test]
    public async Task Scope_CarriesEditionAndBaseUri()
    {
        var f = Create(Schema(edition: SchematronEdition.Iso2025, patterns: Pattern("p", [], Rule("r", "a"))));

        await Assert.That(f.Plan.GlobalScope.Edition).IsEqualTo(SchematronEdition.Iso2025);
        await Assert.That(f.Plan.Patterns[0].Rules[0].Scope.BaseUri).IsEqualTo("scope.sch");
    }

    [Test]
    public async Task Slots_AreReusedAcrossPatternsAndRules()
    {
        var schema = Schema(
            lets: [Let("g", "1")],
            patterns:
            [
                Pattern("p1", [Let("a", "1"), Let("b", "2")], Rule("r1", "x", Let("x", "1"), Let("y", "2"))),
                Pattern("p2", [Let("c", "3")], Rule("r2", "y", Let("z", "3"))),
            ]
        );

        var f = Create(schema);

        await Assert.That(f.Plan.PatternStart).IsEqualTo(1);
        await Assert.That(f.Plan.RuleStart).IsEqualTo(3);
        await Assert.That(f.Plan.SlotCount).IsEqualTo(5);
        await Assert.That(f.Plan.Patterns[0].Lets[0].Slot).IsEqualTo(f.Plan.Patterns[1].Lets[0].Slot);
        await Assert.That(f.Plan.Patterns[0].Rules[0].Lets[0].Slot).IsEqualTo(f.Plan.Patterns[1].Rules[0].Lets[0].Slot);
        await Assert.That(f.Plan.Patterns[0].Rules[0].Lets[1].Slot).IsEqualTo(4);
    }

    [Test]
    public async Task Pattern_OutsideThePhase_IsNotPlanned()
    {
        var schema = Schema(
            phases: [Phase("ph", [], "p1")],
            patterns: [Pattern("p1", [Let("a", "1")], Rule("r1", "x")), Pattern("p2", [Let("b", "1")], Rule("r2", "y"))]
        );

        var f = Create(schema, "ph");

        await Assert.That(f.Plan.Patterns.Count).IsEqualTo(1);
        await Assert.That(f.Plan.Patterns[0].Pattern.Id).IsEqualTo("p1");
    }

    [Test]
    public async Task AllProblems_AreCollected_NotOnlyTheFirst()
    {
        var schema = Schema(
            lets: [Let("x", "1"), Let("x", "2")],
            patterns: Pattern("p", [], Rule("r", "a", Let("y", "$nope")))
        );

        var f = Create(schema);

        await Assert.That(f.Ok).IsFalse();
        await Assert.That(Codes(f.Problems)).IsEqualTo("NES3200,NES3300");
    }

    [Test]
    public async Task Planner_RejectsNullArguments()
    {
        var schema = Schema();
        var binding = new ScopeTestBinding();
        var sink = new FakeProblemSink();
        var selection = new PhaseSelection("#ALL", [], []);

        await Assert
            .That(() => ScopePlanner.TryPlan(null!, selection, binding, sink, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ScopePlanner.TryPlan(schema, null!, binding, sink, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ScopePlanner.TryPlan(schema, selection, null!, sink, out _))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ScopePlanner.TryPlan(schema, selection, binding, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Namespaces_DuplicatePrefix_ArePassedUnchanged_ToEveryScope()
    {
        var schema = Schema(
            namespaces: [Ns("p", "urn:first"), Ns("p", "urn:second")],
            patterns: Pattern("pt", [], Rule("r", "a"))
        );

        var f = Create(schema);

        await Assert.That(f.Ok).IsTrue();
        await Assert.That(f.Plan.Patterns[0].Rules[0].Scope.Namespaces.Count).IsEqualTo(2);
        await Assert.That(f.Plan.Patterns[0].Rules[0].Scope.Namespaces[1].NamespaceName).IsEqualTo("urn:second");
    }

    [Test]
    public async Task PlanRun_RejectsNullArguments()
    {
        var schema = Schema();
        var binding = new ScopeTestBinding();
        var sink = new FakeProblemSink();

        await Assert.That(() => new PlanRun(null!, binding, sink, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new PlanRun(schema, null!, sink, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new PlanRun(schema, binding, null!, [])).Throws<ArgumentNullException>();
        await Assert
            .That(() => new PlanRun(schema, binding, sink, []).Execute(null!, out _))
            .Throws<ArgumentNullException>();
    }
}
