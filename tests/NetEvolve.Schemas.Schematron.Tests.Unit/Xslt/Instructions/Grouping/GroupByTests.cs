namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// <c>xsl:for-each-group group-by</c> (XSLT 2.0 section 14.3): the groups come in order of first appearance, an item with several keys is
/// a member of several groups, keys are compared by <c>eq</c> with NaN equal to itself, and the body runs once per group with the
/// initial item of the group as the focus.
/// </summary>
public class GroupByTests
{
    private static string Problem(DiagnosticCode code, string? constraint = null) =>
        "problem:" + code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task GroupBy_YieldsGroupsInOrderOfFirstAppearance_WithTheExpectedMembers()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=b i=a i=b i=c i=a");

        await Assert.That(harness.Result()).IsEqualTo("i=b i=b;i=a i=a;i=c;");
    }

    [Test]
    public async Task GroupBy_CurrentGroupingKey_IsTheKeyOfTheCurrentGroup()
    {
        var harness = new GroupHarness(By("*", "@k", 10, KeyAndMembers(11)), "i=b i=a i=b i=c i=a");

        await Assert.That(harness.Result()).IsEqualTo("b:i=b i=b;a:i=a i=a;c:i=c;");
    }

    [Test]
    public async Task GroupBy_ItemWithTwoKeys_AppearsInTwoGroups()
    {
        var harness = new GroupHarness(By("*", "@k, @j", 10, KeyAndMembers(11)), "i=a/x i=b/x i=a/y");

        await Assert.That(harness.Result()).IsEqualTo("a:i=a/x i=a/y;x:i=a/x i=b/x;b:i=b/x;y:i=a/y;");
    }

    [Test]
    public async Task GroupBy_EqualKeysOfOneItem_AddTheItemOnce_AndAnUntypedKeyEqualsTheStringOfTheSameText()
    {
        // The key @k is untyped, the literal is a string: they are one key after the cast to a string, so item one is in group a once.
        var harness = new GroupHarness(By("*", "@k, 'a'", 10, KeyAndMembers(11)), "i=a i=b");

        await Assert.That(harness.Result()).IsEqualTo("a:i=a i=b;b:i=b;");
    }

    [Test]
    public async Task GroupBy_ItemWithoutAKey_IsInNoGroup()
    {
        var harness = new GroupHarness(By("*", "@k", 10, KeyAndMembers(11)), "i=a h i=a");

        await Assert.That(harness.Result()).IsEqualTo("a:i=a i=a;");
    }

    [Test]
    public async Task GroupBy_KeysOfNumericTypesCompareByValue_AStringNeverEqualsANumber_AndNaNEqualsNaN()
    {
        var harness = new GroupHarness(
            By("int:1, dbl:1e0, 'a', dbl:NaN, dbl:NaN, '1'", ".", 10, KeyAndMembers(11)),
            string.Empty
        );

        await Assert.That(harness.Result()).IsEqualTo("1:1 1;a:a;NaN:NaN NaN;1:1;");
    }

    [Test]
    public async Task GroupBy_DecimalAndIntegerOfTheSameValue_AreOneKey()
    {
        var harness = new GroupHarness(
            By(
                "int:1, dec:1.0, dec:2.5",
                ".",
                10,
                X("value-of", 11, A("select", "concat(count(current-group()), ';')"))
            )
        );

        await Assert.That(harness.Result()).IsEqualTo("2;1;");
    }

    [Test]
    public async Task GroupBy_KeyOfAnUnsupportedType_IsAnEvaluationProblem()
    {
        var harness = new GroupHarness(By("date:2020-01-01", ".", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed));
        await Assert.That(harness.Output).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GroupBy_KeyExpression_IsEvaluatedWithTheItemPositionAndSizeOfThePopulation()
    {
        var harness = new GroupHarness(By("*", "concat(position(), '-', last())", 10, KeyAndMembers(11)), "i i i");

        await Assert.That(harness.Result()).IsEqualTo("1-3:i;2-3:i;3-3:i;");
    }

    [Test]
    public async Task GroupBy_SelectIsEvaluatedOnceWithTheFocusOfTheFrame()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b");

        _ = harness.Execute();

        await Assert.That(harness.Run.Evaluations[0]).IsEqualTo("*@1/1");
    }

    [Test]
    public async Task GroupBy_EmptyPopulation_RunsTheBodyNever()
    {
        var harness = new GroupHarness(By("()", "@k", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(string.Empty);
        await Assert.That(harness.Run.Writes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Body_Focus_IsTheInitialItem_ItsPositionAmongTheGroups_AndTheNumberOfGroups()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, X("value-of", 11, A("select", "concat(position(), '/', last(), ':', show(.), ';')"))),
            "i=b i=a i=b"
        );

        await Assert.That(harness.Result()).IsEqualTo("1/2:i=b;2/2:i=a;");
    }

    [Test]
    public async Task Slots_AreRestoredAfterTheInstruction_ToWhatTheCallerHad()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b");
        var group = XdmSequence.Create(XdmItem.FromInteger(5));
        var key = XdmSequence.Create(XdmItem.FromString("outer"));
        harness.Run.CurrentGroup = group;
        harness.Run.CurrentGroupingKey = key;

        _ = harness.Execute();

        await Assert.That(harness.Run.CurrentGroup).IsEqualTo(group);
        await Assert.That(harness.Run.CurrentGroupingKey).IsEqualTo(key);
    }

    [Test]
    public async Task Slots_AreRestoredAfterAFailure()
    {
        var harness = new GroupHarness(By("*", "@k", 10, X("value-of", 11, A("select", "error()"))), "i=a");

        var status = harness.Execute();

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(harness.Run.CurrentGroup).IsNull();
        await Assert.That(harness.Run.CurrentGroupingKey).IsNull();
    }

    [Test]
    public async Task Slots_AreAbsentOutsideTheInstruction_AndBoundPerGroupInside()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=a i=b");

        _ = harness.Execute();

        await Assert.That(string.Join(",", harness.Run.Writes)).IsEqualTo("group:2,key:a,group:1,key:b,group:-,key:-");
    }

    [Test]
    public async Task Slots_AreRestoredAfterNestedInstruction()
    {
        var inner = By(
            "current-group()",
            "@j",
            12,
            X("value-of", 13, A("select", "concat(key(current-grouping-key()), '.')"))
        );
        var harness = new GroupHarness(
            By(
                "*",
                "@k",
                10,
                inner,
                X("value-of", 14, A("select", "concat(key(current-grouping-key()), count(current-group()), ';')"))
            ),
            "i=a/x i=a/y i=b/x"
        );

        await Assert.That(harness.Result()).IsEqualTo("x.y.a2;x.b1;");
    }

    [Test]
    public async Task GroupBy_ARunThatCannotHoldTheCurrentGroup_IsAProblemNotASilentWrongResult()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a");
        using var plain = new PlainRun();
        var frame = new XsltFrame(plain) { Focus = new QueryFocus(harness.Document.Root, 1, 1) };

        var status = harness.Executable!.TryExecute(frame, harness.Sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(frame.TakeProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task GroupBy_MoreItemsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b i=c i=d");
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }

    [Test]
    public async Task GroupBy_MoreMembershipsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        // Three items with two keys each make six memberships; the limit is three.
        var harness = new GroupHarness(By("*", "@k, @j", 10, Members(11)), "i=a/x i=b/y i=c/z");
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }

    [Test]
    public async Task GroupBy_MoreKeysOfOneItemThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new GroupHarness(By("*", "'a', 'b', 'c'", 10, Members(11)), "i");
        harness.Frame.MaxSequenceLength = 2;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }

    [Test]
    public async Task GroupBy_ExactlyTheSequenceLimit_Runs()
    {
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b i=a");
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo("i=a i=a;i=b;");
    }

    [Test]
    public async Task GroupBy_SelectProblem_IsReported_AndKeyProblem_IsReported()
    {
        var select = new GroupHarness(By("error()", "@k", 10, Members(11)));
        var key = new GroupHarness(By("*", "error()", 10, Members(11)), "i=a");

        await Assert.That(select.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
        await Assert.That(key.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
    }

    [Test]
    public async Task GroupBy_BodyProblem_EndsTheInstruction()
    {
        var harness = new GroupHarness(
            By("*", "@k", 10, Members(11), X("value-of", 12, A("select", "error()"))),
            "i=a i=b"
        );

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
        await Assert.That(harness.Output).IsEqualTo("i=a;");
    }

    [Test]
    public async Task GroupBy_EveryItem_IsACancellationPoint()
    {
        using var source = new CancellationTokenSource();
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b i=c", cancellationToken: source.Token);
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert.That(() => harness.Execute()).Throws<System.OperationCanceledException>();
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GroupBy_EveryGroup_IsACancellationPoint()
    {
        using var source = new CancellationTokenSource();
        var harness = new GroupHarness(By("*", "@k", 10, Members(11)), "i=a i=b i=c", cancellationToken: source.Token);
        var sink = new CancelOnTextSink(source);

        // The token is canceled while the first group writes; the check before the second group stops the instruction.
        await Assert.That(() => harness.Execute(sink)).Throws<System.OperationCanceledException>();
        await Assert.That(sink.Texts).IsEqualTo(1);
    }

    private sealed class PlainRun : IQueryRun
    {
        public QueryStatus TryEvaluateBoolean(QueryExpression expression, in QueryFocus focus, out bool value)
        {
            value = false;
            return QueryStatus.Ok;
        }

        public QueryStatus TryEvaluate(QueryExpression expression, in QueryFocus focus, ISequenceSink sink) =>
            QueryStatus.Ok;

        public QueryStatus TryBindVariable(int slot, QueryExpression value, in QueryFocus focus) => QueryStatus.Ok;

        public void BindVariable(int slot, string value) { }

        public SchematronProblem GetProblem() => default;

        public void Dispose() { }
    }
}
