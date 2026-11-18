namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System.Threading;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;

/// <summary>
/// The run-time binding of slots (design #150 EP-5): lazy and demand-driven, once per run for the schema and phase level, once
/// per pattern pass for the pattern level, once per firing for the rule level, with a memoized failure, iterative and without
/// allocation for a slot that is already bound.
/// </summary>
public class SlotBinderTests
{
    private static readonly XPathNode Doc = PatternFixture.Node("doc");
    private static readonly XPathNode A1 = PatternFixture.Node("a1");
    private static readonly XPathNode A2 = PatternFixture.Node("a2");

    private sealed class Harness
    {
        internal Harness(ScopeFixture fixture)
        {
            Fixture = fixture;
            Run = new ScopeTestRun();
            Problems = new FakeProblemSink();
            Binder = new SlotBinder(fixture.Plan, Run, new QueryFocus(Doc, 1, 1), Problems);
        }

        internal ScopeFixture Fixture { get; }

        internal ScopeTestRun Run { get; }

        internal FakeProblemSink Problems { get; }

        internal SlotBinder Binder { get; }

        internal ScopePlan Plan => Fixture.Plan;

        internal QueryStatus Ensure(params int[] slots) => Binder.EnsureBound(slots);

        internal void Fire(int pattern, int rule, XPathNode node) =>
            Binder.BeginFiring(Plan.Patterns[pattern].Rules[rule], new QueryFocus(node, 1, 1));
    }

    private static Harness Start(SchematronSchema schema) => new(Create(schema));

    [Test]
    public async Task SchemaLet_ReadsDocument_EvaluatedLazilyOnce()
    {
        var h = Start(Schema(lets: [Let("n", "count(//item)")], patterns: Pattern("p", [], Rule("r", "r"))));

        var first = h.Ensure(0);
        var second = h.Ensure(0);

        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(second).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Run.Binds.Count).IsEqualTo(1);
        await Assert.That(h.Run.Binds[0].Node).IsEqualTo(Doc);
    }

    [Test]
    public async Task UnusedDocumentLet_IsNeverEvaluated()
    {
        var h = Start(Schema(lets: [Let("z", "bad"), Let("n", "count(//item)")]));

        _ = h.Ensure(1);

        await Assert.That(h.Run.Binds.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DocumentLetFocus_IsTheDocumentNode_AlsoAtPatternLevelInsideAFiring()
    {
        var h = Start(Schema(patterns: Pattern("p", [Let("n", "count(//item)")], Rule("r", "a"))));
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        h.Fire(0, 0, A1);

        _ = h.Ensure(h.Plan.PatternStart);

        await Assert.That(h.Run.Binds[0].Node).IsEqualTo(Doc);
    }

    [Test]
    public async Task RuleLet_UsesTheMatchedNodeAsFocus()
    {
        var h = Start(Schema(patterns: Pattern("p", [], Rule("r", "a", Let("v", "string(@n)")))));
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        h.Fire(0, 0, A2);

        _ = h.Ensure(h.Plan.RuleStart);

        await Assert.That(h.Run.Binds[0].Node).IsEqualTo(A2);
    }

    [Test]
    public async Task RuleLet_IsPerFiring()
    {
        var h = Start(Schema(patterns: Pattern("p", [], Rule("r", "a", Let("v", "string(@n)")))));
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        var slot = h.Plan.RuleStart;

        h.Fire(0, 0, A1);
        _ = h.Ensure(slot);
        _ = h.Ensure(slot);
        h.Fire(0, 0, A2);
        _ = h.Ensure(slot);

        await Assert.That(h.Run.Binds.Count).IsEqualTo(2);
        await Assert.That(h.Run.Binds[0].Node).IsEqualTo(A1);
        await Assert.That(h.Run.Binds[1].Node).IsEqualTo(A2);
    }

    [Test]
    public async Task UnreadRuleLet_IsNeverEvaluated()
    {
        var h = Start(Schema(patterns: Pattern("p", [], Rule("r", "a", Let("z", "bad")))));
        h.Binder.BeginPattern(h.Plan.Patterns[0]);

        h.Fire(0, 0, A1);
        h.Fire(0, 0, A2);

        await Assert.That(h.Run.Binds.Count).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Slots_PatternBlockMemo_ResetPerPattern()
    {
        var h = Start(
            Schema(
                patterns:
                [
                    Pattern("p1", [Let("a", "count(//one)")], Rule("r1", "x")),
                    Pattern("p2", [Let("c", "count(//two)")], Rule("r2", "y")),
                ]
            )
        );
        var slot = h.Plan.PatternStart;

        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        _ = h.Ensure(slot);
        _ = h.Ensure(slot);
        h.Binder.BeginPattern(h.Plan.Patterns[1]);
        _ = h.Ensure(slot);
        h.Binder.BeginPattern(h.Plan.Patterns[1]);
        _ = h.Ensure(slot);

        await Assert.That(h.Run.Binds.Count).IsEqualTo(3);
        await Assert.That(h.Run.Binds[0].Text).IsEqualTo("count(//one)");
        await Assert.That(h.Run.Binds[1].Text).IsEqualTo("count(//two)");
    }

    [Test]
    public async Task Let_ReadsOfEarlierLets_AreBoundFirst()
    {
        var h = Start(Schema(lets: [Let("a", "count(//a)"), Let("b", "$a"), Let("c", "$b + $a")]));

        var status = h.Ensure(2);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Run.Binds.ConvertAll(b => b.Slot)).IsEquivalentTo(new[] { 0, 1, 2 });
    }

    [Test]
    public async Task NonConstantLet_TypeMismatch_ReportsNes3201AtFirstUse_ThenMemoized()
    {
        var h = Start(Schema(lets: [Let("n", "string(//item)", "xs:integer")]));

        var first = h.Ensure(0);
        var second = h.Ensure(0);

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.LetType);
        await Assert.That(h.Run.Binds.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RuleLet_Error_ReportedAtFirstRead_OneIssuePerFiring()
    {
        var h = Start(Schema(patterns: Pattern("p", [], Rule("r", "a", Let("z", "bad")))));
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        var slot = h.Plan.RuleStart;

        h.Fire(0, 0, A1);
        _ = h.Ensure(slot);
        _ = h.Ensure(slot);
        h.Fire(0, 0, A2);
        var status = h.Ensure(slot);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301,NES3301");
    }

    [Test]
    public async Task FailedDependency_FailsTheReader_WithoutANewIssue_AndOtherLetsStillBind()
    {
        var h = Start(Schema(lets: [Let("a", "bad"), Let("b", "$a"), Let("c", "count(//c)")]));

        var failed = h.Ensure(1);
        var other = h.Ensure(2);

        await Assert.That(failed).IsEqualTo(QueryStatus.Problem);
        await Assert.That(other).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Run.Binds.ConvertAll(b => b.Slot)).IsEquivalentTo(new[] { 0, 2 });
    }

    [Test]
    public async Task EnsureBound_OfSeveralSlots_BindsAllAndReportsProblemWhenOneFailed()
    {
        var h = Start(Schema(lets: [Let("a", "count(//a)"), Let("b", "bad"), Let("c", "count(//c)")]));

        var status = h.Ensure(0, 1, 2);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(h.Run.Binds.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RuleLevelLiteralLet_TypeMismatch_ReportsNes3201PerFiring_AndContinues()
    {
        var h = Start(
            Schema(patterns: Pattern("p", [], Rule("r", "a", Let("n", "'abc'", "xs:integer"), Let("m", "count(//m)"))))
        );
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        var failed = h.Plan.RuleStart;

        h.Fire(0, 0, A1);
        var first = h.Ensure(failed);
        var other = h.Ensure(failed + 1);
        h.Fire(0, 0, A2);
        var second = h.Ensure(failed);

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(other).IsEqualTo(QueryStatus.Ok);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3201,NES3201");
        await Assert.That(h.Run.Binds.Count).IsEqualTo(1);
    }

    [Test]
    public async Task PatternLevelLiteralLet_TypeMismatch_ReportsNes3201PerPatternPass()
    {
        var h = Start(Schema(patterns: Pattern("p", [Let("n", "'abc'", "xs:integer")], Rule("r", "a"))));

        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        var status = h.Ensure(h.Plan.PatternStart);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3201,NES3201");
    }

    [Test]
    public async Task Param_CallerValue_IsBoundAsLiteral_AndTheDefaultIsNotEvaluated()
    {
        var h = Start(Schema(parameters: [Param("p", "'a'")]));

        var set = h.Binder.SetParameter("p", "b");
        var status = h.Ensure(0);

        await Assert.That(set).IsTrue();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Run.Literals.Count).IsEqualTo(1);
        await Assert.That(h.Run.Literals[0].Value).IsEqualTo("b");
        await Assert.That(h.Run.Binds.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Param_WithoutCallerValue_EvaluatesItsDefault()
    {
        var h = Start(Schema(parameters: [Param("p", "'a'")]));

        var status = h.Ensure(0);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Run.Binds.Count).IsEqualTo(1);
        await Assert.That(h.Run.Literals.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Param_WithoutDefaultAndCallerValue_ReportsParamMissing()
    {
        var h = Start(Schema(parameters: [Param("p")]));

        var status = h.Ensure(0);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3111");
    }

    [Test]
    public async Task SetParameter_UnknownName_IsRefused()
    {
        var h = Start(Schema(lets: [Let("a", "1")], parameters: [Param("p", "'a'")]));

        await Assert.That(h.Binder.SetParameter("a", "x")).IsFalse();
        await Assert.That(h.Binder.SetParameter("nope", "x")).IsFalse();
    }

    [Test]
    public async Task Cycle_ReportsXtde0640AsEvalFailed_OnceAndMemoizes()
    {
        var plan = ManualPlan(
            new ScopeTestExpression("a", null, [1]),
            new ScopeTestExpression("b", null, [0]),
            new ScopeTestExpression("c", null, [])
        );
        var run = new ScopeTestRun();
        var problems = new FakeProblemSink();
        var binder = new SlotBinder(plan, run, new QueryFocus(Doc, 1, 1), problems);

        var status = binder.EnsureBound([0]);
        var again = binder.EnsureBound([1]);
        var unrelated = binder.EnsureBound([2]);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(again).IsEqualTo(QueryStatus.Problem);
        await Assert.That(unrelated).IsEqualTo(QueryStatus.Ok);
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problems.Problems[0].Constraint).IsEqualTo("XTDE0640");
    }

    [Test]
    public async Task Let_Chain_Of10000_BindsWithoutRecursion()
    {
        const int Depth = 10_000;
        var expressions = new ScopeTestExpression[Depth];
        for (var i = 0; i < Depth; i++)
        {
            expressions[i] = new ScopeTestExpression("e", null, i == 0 ? [] : [i - 1]);
        }

        var plan = ManualPlan(expressions);
        var run = new ScopeTestRun();
        var problems = new FakeProblemSink();
        var status = QueryStatus.Problem;

        // 128 KiB hold far fewer than 10,000 nested frames, so only an iterative walk survives.
        var thread = new Thread(
            () =>
            {
                var binder = new SlotBinder(plan, run, new QueryFocus(Doc, 1, 1), problems);
                status = binder.EnsureBound([Depth - 1]);
            },
            128 * 1024
        );
        thread.Start();
        thread.Join();

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(run.Binds.Count).IsEqualTo(Depth);
        await Assert.That(run.Binds[0].Slot).IsEqualTo(0);
        await Assert.That(run.Binds[Depth - 1].Slot).IsEqualTo(Depth - 1);
    }

    [Test]
    public async Task Slot_Lookup_AllocatesNothing()
    {
        var h = Start(
            Schema(
                lets: [Let("a", "count(//a)"), Let("b", "$a")],
                patterns: Pattern("p", [Let("c", "$b")], Rule("r", "x", Let("d", "$c")))
            )
        );
        h.Binder.BeginPattern(h.Plan.Patterns[0]);
        var rule = h.Plan.Patterns[0].Rules[0];
        var focus = new QueryFocus(A1, 1, 1);
        var slots = new[] { 0, 1, h.Plan.PatternStart };
        h.Binder.BeginFiring(rule, in focus);
        _ = h.Binder.EnsureBound(slots);
        var binder = h.Binder;

        AllocationAssert.NoAllocation(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                _ = binder.EnsureBound(slots);
            }
        });

        await Assert.That(binder.EnsureBound(slots)).IsEqualTo(QueryStatus.Ok);
    }

    [Test]
    public async Task Slot_Lookup_AfterEpochChange_AllocatesNothingForGlobalSlots()
    {
        var h = Start(Schema(lets: [Let("a", "count(//a)")], patterns: Pattern("p", [], Rule("r", "x"))));
        var rule = h.Plan.Patterns[0].Rules[0];
        var focus = new QueryFocus(A1, 1, 1);
        _ = h.Ensure(0);
        var binder = h.Binder;
        var slots = new[] { 0 };

        AllocationAssert.NoAllocation(() =>
        {
            binder.BeginFiring(rule, in focus);
            _ = binder.EnsureBound(slots);
        });

        await Assert.That(binder.EnsureBound(slots)).IsEqualTo(QueryStatus.Ok);
    }

    [Test]
    public async Task Binder_RejectsNullArguments()
    {
        var f = Create(Schema());
        var run = new ScopeTestRun();
        var sink = new FakeProblemSink();
        var focus = new QueryFocus(Doc, 1, 1);

        await Assert.That(() => new SlotBinder(null!, run, focus, sink)).Throws<ArgumentNullException>();
        await Assert.That(() => new SlotBinder(f.Plan, null!, focus, sink)).Throws<ArgumentNullException>();
        await Assert.That(() => new SlotBinder(f.Plan, run, focus, null!)).Throws<ArgumentNullException>();
    }

    private static ScopePlan ManualPlan(params ScopeTestExpression[] expressions)
    {
        var globals = new VariableSlot[expressions.Length];
        for (var i = 0; i < expressions.Length; i++)
        {
            globals[i] = new VariableSlot("v" + i, i, ScopeLevel.Schema, FakeModel.Origin, expressions[i], null, null);
        }

        var scope = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);
        return new ScopePlan(globals, [], [], scope, expressions.Length, expressions.Length, expressions.Length);
    }
}
