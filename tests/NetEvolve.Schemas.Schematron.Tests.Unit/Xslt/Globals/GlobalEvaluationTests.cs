namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Globals.GlobalFixture;

/// <summary>
/// The run time of global <c>xsl:variable</c> and <c>xsl:param</c> (XSLT 2.0 section 9.5 and 9.8, design #150 EP-5): lazy and demand
/// driven, once per validated document, with the circularity error XTDE0640, with the value of a parameter from the builder, and
/// visible inside a function body and inside the test of an assertion.
/// </summary>
public class GlobalEvaluationTests
{
    [Test]
    public async Task Variable_IsEvaluatedAtTheFirstUse_AndExactlyOncePerDocument()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("variable", 10, A("name", "g"), A("select", "1 + 1")));

        await Assert.That(h.Binds("g")).IsEqualTo(0);
        var first = h.Ensure("g");
        var second = h.Ensure("g");
        var third = h.Ensure("g");

        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(second).IsEqualTo(QueryStatus.Ok);
        await Assert.That(third).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Binds("g")).IsEqualTo(1);
        await Assert.That(h.Value("g")).IsEqualTo("2");
    }

    [Test]
    public async Task Variable_IsEvaluatedAgainForTheNextDocument_BecauseEveryRunHasItsOwnValues()
    {
        var firstDocument = new GlobalFixture().Start(
            EmptySchema(),
            X("variable", 10, A("name", "g"), A("select", "1"))
        );
        var secondDocument = new GlobalFixture().Start(
            EmptySchema(),
            X("variable", 10, A("name", "g"), A("select", "1"))
        );

        _ = firstDocument.Ensure("g");
        _ = firstDocument.Ensure("g");
        _ = secondDocument.Ensure("g");

        await Assert.That(firstDocument.Binds("g")).IsEqualTo(1);
        await Assert.That(secondDocument.Binds("g")).IsEqualTo(1);
    }

    [Test]
    public async Task Variable_ThatNothingReads_IsNeverEvaluated()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "unused"), A("select", "1 idiv 0")),
            X("variable", 11, A("name", "used"), A("select", "1"))
        );

        _ = h.Ensure("used");

        await Assert.That(h.Binds("unused")).IsEqualTo(0);
        await Assert.That(h.Binds("used")).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Variable_ThatReadsAnother_BindsItsDependenciesFirst_EachOnce()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "a"), A("select", "$b + $c")),
            X("variable", 11, A("name", "b"), A("select", "$c + 1")),
            X("variable", 12, A("name", "c"), A("select", "5"))
        );

        var status = h.Ensure("a");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("a")).IsEqualTo("11");
        await Assert.That(h.Binds("a")).IsEqualTo(1);
        await Assert.That(h.Binds("b")).IsEqualTo(1);
        await Assert.That(h.Binds("c")).IsEqualTo(1);
        await Assert
            .That(string.Join(",", h.Run.Events))
            .IsEqualTo($"bind:{h.Slot("c")},bind:{h.Slot("b")},bind:{h.Slot("a")}");
    }

    [Test]
    public async Task Variable_ASchemaLetMayReadAGlobalThatIsDeclaredAfterIt()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(Schema(lets: [Let("a", "$g + 1")]), X("variable", 10, A("name", "g"), A("select", "1")));

        var status = h.Ensure("a");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("a")).IsEqualTo("2");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_OfTwoGlobals_IsXtde0640_ReportedOnceAndMemoized()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "a"), A("select", "$b")),
            X("variable", 11, A("name", "b"), A("select", "$a"))
        );

        var first = h.Ensure("a");
        var again = h.Ensure("a");
        var other = h.Ensure("b");

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(again).IsEqualTo(QueryStatus.Problem);
        await Assert.That(other).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Binds("a")).IsEqualTo(0);
        await Assert.That(h.Binds("b")).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_OfAGlobalThatReadsItself_IsXtde0640()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("variable", 10, A("name", "g"), A("select", "$g + 1")));

        var status = h.Ensure("g");

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Problems.Problems[0].Location.Line).IsEqualTo(10);
    }

    [Test]
    public async Task Circularity_ThroughAChainOfThree_IsXtde0640()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "a"), A("select", "$b")),
            X("variable", 11, A("name", "b"), A("select", "$c")),
            X("variable", 12, A("name", "c"), A("select", "$a"))
        );

        var status = h.Ensure("b");

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
    }

    [Test]
    public async Task Circularity_BetweenAGlobalAndASchemaLet_IsXtde0640()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(Schema(lets: [Let("a", "$g")]), X("variable", 10, A("name", "g"), A("select", "$a")));

        var status = h.Ensure("a");

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
    }

    [Test]
    public async Task Circularity_OfAGlobalThatCallsAFunctionReadingIt_IsXtde0640_ReportedOnceAndMemoized()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "g"), A("select", "f:x()")),
            Fx.Simple("x", "xs:integer", [], "$g", 20)
        );

        var first = h.Ensure("g");
        var again = h.Ensure("g");

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(again).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Problems.Problems[0].Location.Line).IsEqualTo(10);
        await Assert.That(h.Binds("g")).IsEqualTo(0);
        await Assert.That(h.Run.Active).IsEqualTo(0);
        await Assert.That(h.Run.MaxActive).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_OfASchemaLetThatCallsAFunctionReadingIt_IsXtde0640()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(Schema(lets: [Let("g", "f:x()")]), Fx.Simple("x", "xs:integer", [], "$g", 20));

        var status = h.Ensure("g");

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Run.MaxActive).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_ThroughTwoFunctionsAndTwoGlobals_IsXtde0640_OnceForTheWholeCycle()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "a"), A("select", "f:x()")),
            X("variable", 11, A("name", "b"), A("select", "f:y()")),
            Fx.Simple("x", "xs:integer", [], "$b", 20),
            Fx.Simple("y", "xs:integer", [], "$a", 21)
        );

        var first = h.Ensure("a");
        var other = h.Ensure("b");

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(other).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Run.Active).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_ThroughAFunction_DoesNotDisturbAVariableThatDoesNotTakePart()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "g"), A("select", "f:x()")),
            X("variable", 11, A("name", "fine"), A("select", "1 + 1")),
            Fx.Simple("x", "xs:integer", [], "$g", 20)
        );

        var cyclic = h.Ensure("g");
        var fine = h.Ensure("fine");

        await Assert.That(cyclic).IsEqualTo(QueryStatus.Problem);
        await Assert.That(fine).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("fine")).IsEqualTo("2");
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
    }

    [Test]
    public async Task Evaluation_OfAGlobalWhoseFunctionBindsAnotherGlobal_WhileAnOuterWalkIsPending_IsNoCycle()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "r"), A("select", "$a + 1")),
            X("variable", 11, A("name", "a"), A("select", "f:x()")),
            X("variable", 12, A("name", "b"), A("select", "2")),
            Fx.Simple("x", "xs:integer", [], "$b", 20)
        );

        var first = h.Ensure("r");
        var second = h.Ensure("r");

        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(second).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("r")).IsEqualTo("3");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_ThroughAFunction_WhoseSlotIsBelowAnotherSlotOnTheWalk_IsXtde0640_ReportedOnce()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "r"), A("select", "$a + 1")),
            X("variable", 11, A("name", "a"), A("select", "f:x()")),
            Fx.Simple("x", "xs:integer", [], "$r", 20)
        );

        var first = h.Ensure("r");
        var second = h.Ensure("r");

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
        await Assert.That(h.Run.Active).IsEqualTo(0);
    }

    [Test]
    public async Task Circularity_IsOnlyAnErrorWhenTheCircularVariableIsUsed()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "a"), A("select", "$b")),
            X("variable", 11, A("name", "b"), A("select", "$a")),
            X("variable", 12, A("name", "fine"), A("select", "1"))
        );

        var status = h.Ensure("fine");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Variable_AFailingValue_IsReportedOnceAtTheFirstUse_AndStaysFailed()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("variable", 10, A("name", "g"), A("select", "1 idiv 0")));

        var first = h.Ensure("g");
        var second = h.Ensure("g");

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Param_WithoutAValueFromTheCaller_UsesTheDefault()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("param", 10, A("name", "p"), A("select", "'default'")));

        var status = h.Ensure("p");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("p")).IsEqualTo("default");
        await Assert.That(h.Binds("p")).IsEqualTo(1);
    }

    [Test]
    public async Task Param_ValueSetThroughTheBuilder_OverridesTheDefault()
    {
        var pipeline = new FakePipeline();
        _ = SchemaValidator
            .ForSchematron("<schema/>".AsSpan())
            .WithParameter("p", "given")
            .UsePipeline(pipeline)
            .BuildNow();
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("param", 10, A("name", "p"), A("select", "'default'")));

        foreach (var parameter in pipeline.LastSettings!.Parameters)
        {
            await Assert.That(h.Binder.SetParameter(parameter.Key, parameter.Value)).IsTrue();
        }

        var status = h.Ensure("p");

        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("p")).IsEqualTo("given");
        await Assert.That(h.Binds("p")).IsEqualTo(0);
    }

    [Test]
    public async Task Param_OverriddenThroughTheBuilder_IsSeenByAVariableThatReadsIt()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("param", 10, A("name", "p"), A("select", "'default'")),
            X("variable", 11, A("name", "v"), A("select", "concat($p, '!')"))
        );

        _ = h.Binder.SetParameter("p", "given");
        _ = h.Ensure("v");

        await Assert.That(h.Value("v")).IsEqualTo("given!");
    }

    [Test]
    public async Task SetParameter_ForAGlobalVariable_IsRefused_BecauseOnlyAParamTakesAValueFromTheCaller()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("variable", 10, A("name", "g"), A("select", "1")));

        await Assert.That(h.Binder.SetParameter("g", "given")).IsFalse();
        _ = h.Ensure("g");
        await Assert.That(h.Value("g")).IsEqualTo("1");
    }

    [Test]
    public async Task Param_Required_WithoutAValueAtBuildTime_IsTheCodedBuildError()
    {
        var fixture = new GlobalFixture();
        var table = fixture.Table(0, X("param", 10, A("name", "p"), A("required", "yes")));
        var pipeline = new FakePipeline();
        _ = SchemaValidator.ForSchematron("<schema/>".AsSpan()).UsePipeline(pipeline).BuildNow();
        var problems = new FakeProblemSink();

        var ok = table.CheckParameters(pipeline.LastSettings!.Parameters, problems);

        await Assert.That(ok).IsFalse();
        await Assert.That(Codes(problems)).IsEqualTo("NES3111/XTDE0050");
    }

    [Test]
    public async Task Param_Required_WithAValueFromTheBuilder_IsBoundWithoutADefault()
    {
        var pipeline = new FakePipeline();
        _ = SchemaValidator
            .ForSchematron("<schema/>".AsSpan())
            .WithParameter("p", "given")
            .UsePipeline(pipeline)
            .BuildNow();
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("param", 10, A("name", "p"), A("required", "yes")));

        var checkedOk = h.Table.CheckParameters(pipeline.LastSettings!.Parameters, new FakeProblemSink());
        _ = h.Binder.SetParameter("p", pipeline.LastSettings.Parameters["p"]);
        var status = h.Ensure("p");

        await Assert.That(checkedOk).IsTrue();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(h.Value("p")).IsEqualTo("given");
    }

    [Test]
    public async Task Param_Required_UsedWithoutAValue_StillFailsAtRunTime_AsAGuardIfTheBuildCheckWasSkipped()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(EmptySchema(), X("param", 10, A("name", "p"), A("required", "yes")));

        var status = h.Ensure("p");

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3111");
    }

    [Test]
    public async Task Global_IsVisibleInsideTheBodyOfAFunction_AndBoundOnceForManyCalls()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("variable", 10, A("name", "g"), A("select", "40 + 2")),
            Fx.Simple("answer", "xs:integer", [], "$g", 20)
        );

        var first = h.Test("f:answer() = 42");
        var second = h.Test("f:answer() = 42");

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsTrue();
        await Assert.That(h.Binds("g")).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Global_ParamIsVisibleInsideTheBodyOfAFunction_WithTheValueFromTheCaller()
    {
        var fixture = new GlobalFixture();
        var h = fixture.Start(
            EmptySchema(),
            X("param", 10, A("name", "limit"), A("select", "5")),
            Fx.Simple("label", "xs:string", [], "concat('limit=', $limit)", 20)
        );

        _ = h.Binder.SetParameter("limit", "3");

        await Assert.That(h.Test("f:label() = 'limit=3'")).IsTrue();
        await Assert.That(h.Binds("limit")).IsEqualTo(0);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Global_IsVisibleInsideTheTestOfAnAssertion()
    {
        var fixture = new GlobalFixture();
        var schema = Schema(patterns: Pattern("p", [], Rule("r", "order")));
        var h = fixture.Start(schema, X("variable", 10, A("name", "g"), A("select", "1 + 1")));
        var rule = h.Plan.Patterns[0].Rules[0];
        var sink = new RecordingReportSink();
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        using var evaluator = new AssertionEvaluator(
            h.Run,
            h.Binder,
            new IssueMapper(collector, sink),
            h.Problems,
            "file:///instance.xml",
            1000
        );
        var passing = Compile(fixture, rule, "$g = 2");
        var failing = Compile(fixture, rule, "$g = 3");
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);

        var silent = evaluator.Evaluate(passing, in focus);
        var fired = evaluator.Evaluate(failing, in focus);

        await Assert.That(silent).IsEqualTo(AssertionOutcome.Silent);
        await Assert.That(fired).IsEqualTo(AssertionOutcome.Fired);
        await Assert.That(h.Binds("g")).IsEqualTo(1);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Global_IsVisibleInsideTheTestOfAnAssertion_AndACircularOneIsAProblemOfThatAssertion()
    {
        var fixture = new GlobalFixture();
        var schema = Schema(patterns: Pattern("p", [], Rule("r", "order")));
        var h = fixture.Start(
            schema,
            X("variable", 10, A("name", "a"), A("select", "$b")),
            X("variable", 11, A("name", "b"), A("select", "$a"))
        );
        var rule = h.Plan.Patterns[0].Rules[0];
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        using var evaluator = new AssertionEvaluator(
            h.Run,
            h.Binder,
            new IssueMapper(collector, new RecordingReportSink()),
            h.Problems,
            "file:///instance.xml",
            1000
        );
        var assertion = Compile(fixture, rule, "$a = 1");
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);

        var outcome = evaluator.Evaluate(assertion, in focus);

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Problem);
        await Assert.That(Codes(h.Problems)).IsEqualTo("NES3301/XTDE0640");
    }

    private static CompiledAssertion Compile(GlobalFixture fixture, RuleScope rule, string test)
    {
        var assertion = AssertionModel.MakeAssert(test, AssertionModel.Message("message"));
        var at = new AssertionPosition(1, 1, 1);
        return AssertionCompiler.TryCompile(
            assertion,
            rule,
            EmptySchema(),
            fixture.Binding,
            in at,
            fixture.Problems,
            out var compiled
        )
            ? compiled
            : throw new InvalidOperationException("The assertion does not compile: " + Codes(fixture.Problems));
    }
}
