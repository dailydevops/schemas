namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>The reported node of a finding (design #137 AP-14): the context node, or the first node of <c>subject</c>.</summary>
public class SubjectTests
{
    private static (int Line, int Column) Where(AssertionHarness h) =>
        (h.Sink.Findings[0].Issue!.Location.Line, h.Sink.Findings[0].Issue!.Location.Column);

    [Test]
    public async Task WithoutSubject_ReportsTheContextNode_AndEvaluatesNoSubject()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("false", Message("m")));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((5, 3));
        await Assert.That(h.Sink.Findings[0].Issue!.Location.Source).IsEqualTo(AssertionHarness.Source);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["false"]);
    }

    [Test]
    public async Task Subject_ChangesTheReportedLocation()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("item2")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order1");

        await Assert.That(Where(h)).IsEqualTo((4, 5));
    }

    [Test]
    public async Task Subject_FirstNodeWins()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("item1"), AssertionHarness.NodeItem("item2")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order1");

        await Assert.That(Where(h)).IsEqualTo((3, 5));
    }

    [Test]
    public async Task Subject_DoesNotChangeTheFocusOfTheMessage()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("item2")];
        var assertion = h.Compile(MakeAssert("false", Message(Name()), subject: "subj"));

        _ = h.Evaluate(assertion, "order1");

        await Assert.That(h.Sink.Findings[0].Message).IsEqualTo("order");
    }

    [Test]
    public async Task Subject_AttributeNode_UsesThePositionOfItsElement()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("id")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((2, 3));
    }

    [Test]
    public async Task Subject_DocumentNode_IsPositionZero()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("doc")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((0, 0));
    }

    [Test]
    public async Task Subject_EmptyResult_FallsBackToContext()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((5, 3));
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Subject_NonNodeResult_ReportsEvalFailed_FallsBackToContext()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [XdmItem.FromString("not a node")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        var outcome = h.Evaluate(assertion, "order2");

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Fired);
        await Assert.That(Where(h)).IsEqualTo((5, 3));
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Subject_DynamicError_ReportsEvalFailed()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "bad"));

        var outcome = h.Evaluate(assertion, "order2");

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Fired);
        await Assert.That(Where(h)).IsEqualTo((5, 3));
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
        await Assert.That(h.Sink.Findings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Subject_ReadingAFailedSlot_AddsNoProblem_AndFallsBackToContext()
    {
        using var h = new AssertionHarness(variables: ["x"]);
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "$x"));
        h.Binder.Failing.Add(0);

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((5, 3));
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(0);
        await Assert.That(h.Binder.Bound).IsEquivalentTo([0]);
    }

    [Test]
    public async Task Subject_OfTheRule_IsUsedWhenTheAssertionHasNone()
    {
        using var h = new AssertionHarness(ruleSubject: "ruleSubj");
        h.Binding.Items["ruleSubj"] = [AssertionHarness.NodeItem("item1")];
        var assertion = h.Compile(MakeAssert("false", Message("m")));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((3, 5));
    }

    [Test]
    public async Task Subject_OfTheAssertion_WinsOverTheSubjectOfTheRule()
    {
        using var h = new AssertionHarness(ruleSubject: "ruleSubj");
        h.Binding.Items["ruleSubj"] = [AssertionHarness.NodeItem("item1")];
        h.Binding.Items["ownSubj"] = [AssertionHarness.NodeItem("item2")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "ownSubj"));

        _ = h.Evaluate(assertion, "order2");

        await Assert.That(Where(h)).IsEqualTo((4, 5));
        await Assert
            .That(h.Binding.Compiled.Exists(c => string.Equals(c.Text, "ruleSubj", StringComparison.Ordinal)))
            .IsFalse();
    }

    [Test]
    public async Task Subject_IsCompiledAsNodes_InTheScopeOfTheRule()
    {
        using var h = new AssertionHarness();
        _ = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        var subject = h.Binding.Compiled.Find(c => string.Equals(c.Text, "subj", StringComparison.Ordinal));

        await Assert.That(subject.Kind).IsEqualTo(QueryExpressionKind.Nodes);
        await Assert.That(ReferenceEquals(subject.Scope, h.Rule.Scope)).IsTrue();
    }

    [Test]
    public async Task Subject_LocationPath_FollowsTheSubjectNode()
    {
        using var h = new AssertionHarness();
        h.Binding.Items["subj"] = [AssertionHarness.NodeItem("item2")];
        var assertion = h.Compile(MakeAssert("false", Message("m"), subject: "subj"));

        _ = h.Evaluate(assertion, "order1");

        await Assert
            .That(h.Sink.Findings[0].Location)
            .IsEqualTo(
                "/*[local-name()='root' and namespace-uri()=''][1]"
                    + "/*[local-name()='order' and namespace-uri()=''][1]"
                    + "/*[local-name()='item' and namespace-uri()=''][2]"
            );
    }
}
