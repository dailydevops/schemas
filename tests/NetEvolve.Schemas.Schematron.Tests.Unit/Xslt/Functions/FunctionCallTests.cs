namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// Calling compiled functions (XSLT 2.0 section 10.3, design #150 EP-13 and EP-16): overloads by arity, the function conversion
/// rules for arguments and result, the focus and the slots of a call, the depth limit, and cancellation.
/// </summary>
public class FunctionCallTests
{
    private static async Task<long> CallInteger(FunctionFixture fixture, string name, params long[] arguments)
    {
        var (_, caller) = fixture.CreateRun();
        var function = Find(fixture, name, arguments.Length);
        var values = new XdmSequence[arguments.Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = Int(arguments[i]);
        }

        var status = caller.TryCall(function, values, out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        return MiniQuery.Integer(result);
    }

    private static XsltFunctionComponent Find(FunctionFixture fixture, string name, int arity)
    {
        _ = fixture.Table.TryGet(new XPathQName(Functions, name), arity, out var function);
        return function;
    }

    [Test]
    public async Task Call_TwoFunctionsWithTheSameNameAndDifferentArity_AreBothCallable()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Simple("inc", "xs:integer", ["a"], "$a + 1", 10),
            Simple("inc", "xs:integer", ["a", "b"], "$a + $b", 20)
        );

        await Assert.That(fixture.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(await CallInteger(fixture, "inc", 5)).IsEqualTo(6);
        await Assert.That(await CallInteger(fixture, "inc", 5, 10)).IsEqualTo(15);
        await Assert.That(fixture.Table.TryGet(new XPathQName(Functions, "inc"), 3, out _)).IsFalse();
    }

    [Test]
    public async Task Call_OneFunctionCallingTheOtherOverload_ResolvesByArity()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Simple("twice", "xs:integer", ["a"], "f:twice($a, 2)", 10),
            Simple("twice", "xs:integer", ["a", "n"], "$a * $n", 20)
        );

        await Assert.That(await CallInteger(fixture, "twice", 21)).IsEqualTo(42);
    }

    [Test]
    public async Task Call_ArgumentOfTheWrongType_RaisesTheXPathTypeErrorAndNamesTheFunction()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Simple("inc", "xs:integer", ["a"], "$a + 1", 10));
        var (_, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "inc", 1), [Str("x")], out _);

        var problem = caller.GetProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XPTY0004");
        await Assert.That(problem.Message).Contains("f:inc").And.Contains("XTTE0790").And.Contains("'a'");
        await Assert.That(problem.Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Call_ArgumentConversion_FollowsTheOccurrenceAndPromotionRules()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Function("f:opt", "xs:integer*", 10, Param("a", "xs:integer?", 11), Sequence("$a", 12)),
            Function("f:one", null, 20, Param("a", "xs:integer", 21), Sequence("$a", 22)),
            Function("f:dbl", "xs:double", 30, Param("a", "xs:double", 31), Sequence("$a", 32)),
            Function("f:many", "xs:string+", 40, Param("a", "xs:string+", 41), Sequence("$a", 42))
        );
        var (_, caller) = fixture.CreateRun();

        var emptyToOptional = caller.TryCall(Find(fixture, "opt", 1), [XdmSequence.Empty], out var optional);
        var emptyToOne = caller.TryCall(Find(fixture, "one", 1), [XdmSequence.Empty], out _);
        var twoToOne = caller.TryCall(
            Find(fixture, "one", 1),
            [XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)])],
            out _
        );
        var promoted = caller.TryCall(Find(fixture, "dbl", 1), [Int(3)], out var asDouble);
        var emptyToMany = caller.TryCall(Find(fixture, "many", 1), [XdmSequence.Empty], out _);

        await Assert.That(emptyToOptional).IsEqualTo(QueryStatus.Ok);
        await Assert.That(optional.Count).IsEqualTo(0);
        await Assert.That(emptyToOne).IsEqualTo(QueryStatus.Problem);
        await Assert.That(twoToOne).IsEqualTo(QueryStatus.Problem);
        await Assert.That(promoted).IsEqualTo(QueryStatus.Ok);
        await Assert.That(asDouble[0].TryGetDouble(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(3d);
        await Assert.That(emptyToMany).IsEqualTo(QueryStatus.Problem);
    }

    [Test]
    public async Task Call_ParamWithoutAs_AcceptsAnyValue()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Function("f:any", null, 10, Param("a", null, 11), Sequence("$a", 12)));
        var (_, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "any", 1), [Str("x")], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(MiniQuery.Text(result)).IsEqualTo("x");
    }

    [Test]
    public async Task Call_ResultOfTheWrongType_RaisesTheXPathTypeError()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Function("f:bad", "xs:integer", 10, Sequence("'text'", 11)));
        var (_, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "bad", 0), [], out _);

        var problem = caller.GetProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(problem.Constraint).IsEqualTo("XPTY0004");
        await Assert.That(problem.Message).Contains("f:bad").And.Contains("XTTE0780");
    }

    [Test]
    public async Task Call_FunctionWithoutAs_ReturnsTheSequenceAsEvaluated_AndSequencesConcatenate()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Function("f:list", null, 10, Sequence("1, 2", 11), Sequence("'x'", 12), Sequence("()", 13))
        );
        var (_, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "list", 0), [], out var result);

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(MiniQuery.Text(XdmSequence.Create(result[2]))).IsEqualTo("x");
    }

    [Test]
    public async Task Call_BodyErrors_AreReportedAsProblemsOfTheRun()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Simple("bad", "xs:integer", ["a"], "$a idiv 0", 10));
        var (_, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "bad", 1), [Int(1)], out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(caller.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(caller.Depth).IsEqualTo(0);
    }

    [Test]
    public async Task Call_BodyEvaluatesWithoutAFocus_AndAccessToTheContextItemIsAnError()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Function("f:ok", null, 10, Variable("v", "1", null, 11), Sequence("$v", 12)),
            Function("f:dot", null, 20, Sequence(".", 21))
        );
        var (run, caller) = fixture.CreateRun();

        _ = caller.TryCall(Find(fixture, "ok", 0), [], out _);
        var dot = caller.TryCall(Find(fixture, "dot", 0), [], out _);

        await Assert.That(run.Focuses.Count).IsEqualTo(3);
        foreach (var focus in run.Focuses)
        {
            await Assert.That(focus.Node.Tree is null).IsTrue();
            await Assert.That(focus.Position).IsEqualTo(0);
            await Assert.That(focus.Size).IsEqualTo(0);
        }

        await Assert.That(dot).IsEqualTo(QueryStatus.Problem);
        await Assert.That(caller.GetProblem().Constraint).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task Call_EntersAndLeavesTheFunctionScope_AroundEveryCallAlsoOnFailure()
    {
        var fixture = Create();
        _ = fixture.CompileAll(
            Simple("down", "xs:integer", ["n"], "if ($n = 0) then 0 else f:down($n - 1)", 10),
            Simple("fail", "xs:integer", ["n"], "$n idiv 0", 20)
        );
        var (run, caller) = fixture.CreateRun();

        _ = caller.TryCall(Find(fixture, "down", 1), [Int(2)], out _);
        var enters = run.Count("enter");
        var exits = run.Count("exit");
        _ = caller.TryCall(Find(fixture, "fail", 1), [Int(2)], out _);

        await Assert.That(enters).IsEqualTo(3);
        await Assert.That(exits).IsEqualTo(3);
        await Assert.That(run.Count("exit")).IsEqualTo(4);
        await Assert.That(run.Active).IsEqualTo(0);
    }

    [Test]
    public async Task Call_Recursion_KeepsTheSlotsOfEveryActivationApart()
    {
        var fixture = Create("g");
        _ = fixture.CompileAll(
            Function(
                "f:sum",
                "xs:integer",
                10,
                Param("n", "xs:integer", 11),
                Variable("m", "$n - 1", "xs:integer", 12),
                Sequence("if ($n = 0) then 0 else f:sum($m) + $n", 13)
            )
        );

        await Assert.That(await CallInteger(fixture, "sum", 4)).IsEqualTo(10);
    }

    [Test]
    public async Task Call_RestoresTheSlotsOfTheCaller_AndLeavesUnboundSlotsUnbound()
    {
        var fixture = Create("g");
        _ = fixture.CompileAll(Simple("inc", "xs:integer", ["a"], "$a + 1", 10));
        var (run, caller) = fixture.CreateRun();
        run.SetValue(1, Int(99));

        _ = caller.TryCall(Find(fixture, "inc", 1), [Int(1)], out _);

        await Assert.That(run.TryGetValue(1, out var restored)).IsTrue();
        await Assert.That(MiniQuery.Integer(restored)).IsEqualTo(99);
        run.ClearValue(1);
        _ = caller.TryCall(Find(fixture, "inc", 1), [Int(1)], out _);
        await Assert.That(run.TryGetValue(1, out _)).IsFalse();
    }

    [Test]
    public async Task Call_UnboundedRecursion_EndsWithTheCodedDepthLimitError()
    {
        var fixture = Create();
        _ = fixture.CompileAll(Simple("forever", "xs:integer", ["n"], "f:forever($n + 1)", 10));
        var (run, caller) = fixture.CreateRun();

        var status = caller.TryCall(Find(fixture, "forever", 1), [Int(0)], out _);

        var problem = caller.GetProblem();
        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
        await Assert.That(problem.Code.ToString()).IsEqualTo("NES3503");
        await Assert.That(problem.Message).Contains("f:forever").And.Contains("128");
        await Assert.That(run.MaxActive).IsEqualTo(XsltFunctionCaller.DefaultMaxDepth);
        await Assert.That(caller.Depth).IsEqualTo(0);
        await Assert.That(run.Active).IsEqualTo(0);
    }

    [Test]
    [Arguments(2, true)]
    [Arguments(3, false)]
    public async Task Call_DepthLimit_AllowsExactlyTheConfiguredNumberOfNestedCalls(int n, bool inside)
    {
        var fixture = Create();
        _ = fixture.CompileAll(Simple("down", "xs:integer", ["n"], "if ($n = 0) then 0 else f:down($n - 1)", 10));
        var (_, caller) = fixture.CreateRun(maxDepth: 3);

        var status = caller.TryCall(Find(fixture, "down", 1), [Int(n)], out _);

        await Assert.That(status == QueryStatus.Ok).IsEqualTo(inside);
        if (!inside)
        {
            await Assert.That(caller.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationLimitStop);
        }
    }

    [Test]
    public async Task Call_DepthLimit_IsGuardedByTheCeiling()
    {
        var fixture = Create();

        await Assert.That(() => fixture.CreateRun(maxDepth: 0)).Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => fixture.CreateRun(maxDepth: XsltFunctionCaller.MaxDepthCeiling + 1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Call_CancellationIsCheckedAtEveryCallEntry(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixture = Create();
        _ = fixture.CompileAll(Simple("down", "xs:integer", ["n"], "if ($n = 0) then 0 else f:down($n - 1)", 10));
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var (_, caller) = fixture.CreateCancellableRun(XsltFunctionCaller.DefaultMaxDepth, source.Token);
        var function = Find(fixture, "down", 1);

        var before = caller.TryCall(function, [Int(1)], out _);
        await source.CancelAsync();

        await Assert.That(before).IsEqualTo(QueryStatus.Ok);
        await Assert.That(() => caller.TryCall(function, [Int(1)], out _)).Throws<OperationCanceledException>();
        await Assert.That(caller.Depth).IsEqualTo(0);
    }

    [Test]
    public async Task Call_Guards_RejectNullArguments()
    {
        var fixture = Create();
        var (run, _) = fixture.CreateRun();
        var binder = new NoBinder();

        await Assert
            .That(() => new XsltFunctionCaller(null!, binder, null!, CancellationToken.None, 1))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltFunctionCaller(run, null!, null!, CancellationToken.None, 1))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new XsltFunctionCaller(run, binder, null!, CancellationToken.None, 1))
            .Throws<ArgumentNullException>();
        await Assert.That(() => fixture.CreateRun().Caller.TryCall(null!, [], out _)).Throws<ArgumentNullException>();
    }

    private sealed class NoBinder : ISlotBinder
    {
        public QueryStatus EnsureBound(ReadOnlySpan<int> slots) => QueryStatus.Ok;
    }
}
