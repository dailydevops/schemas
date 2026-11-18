namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>The finding that a fired assertion delivers and the issue it maps to.</summary>
public class FindingTests
{
    [Test]
    public async Task FailedAssert_DeliversAnErrorFinding_WithItsIdentity()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "7";
        var assertion = h.Compile(
            MakeAssert("false", Message(Text("bad "), ValueOf("id")), id: "a1"),
            new AssertionPosition(2, 3, 4)
        );

        var outcome = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Fired);
        await Assert.That(finding.IsReport).IsFalse();
        await Assert.That(finding.Id).IsEqualTo("a1");
        await Assert.That(finding.RuleId).IsEqualTo("r1");
        await Assert.That(finding.Constraint).IsEqualTo("a1");
        await Assert.That(finding.Test).IsEqualTo("false");
        await Assert.That(finding.Message).IsEqualTo("bad 7");
        await Assert.That(finding.Issue).IsNotNull();
        await Assert.That(finding.Issue!.Code).IsEqualTo(SchematronDiagnosticCodes.AssertionFailed);
        await Assert.That(finding.Issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(finding.Issue.Message).IsEqualTo("bad 7");
        await Assert.That(finding.Issue.Constraint).IsEqualTo("a1");
        await Assert.That(h.Collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SuccessfulReport_DeliversAWarningFinding()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeReport("true", Message("seen")));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.IsReport).IsTrue();
        await Assert.That(finding.Issue!.Code).IsEqualTo(SchematronDiagnosticCodes.ReportFired);
        await Assert.That(finding.Issue.Severity).IsEqualTo(IssueSeverity.Warning);
    }

    [Test]
    [Arguments("a1", 0, "a1", "a1")]
    [Arguments("a1", 2, "a1#i2", "a1#i2")]
    [Arguments(null, 0, "", "r1#a3")]
    public async Task Identity_FollowsTheAssertionIdentifier(
        string? authorId,
        int expansion,
        string id,
        string constraint
    )
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(
            MakeAssert("false", Message("m"), id: authorId),
            new AssertionPosition(1, 2, 3, expansion)
        );

        _ = h.Evaluate(assertion);

        await Assert.That(h.Sink.Findings[0].Id).IsEqualTo(id);
        await Assert.That(h.Sink.Findings[0].Constraint).IsEqualTo(constraint);
        await Assert
            .That(assertion.Constraint)
            .IsEqualTo(AssertionIdentifier.Create(authorId, "r1", 1, 2, 3, expansion));
    }

    [Test]
    public async Task Identity_WithoutAnyId_IsGeneratedFromTheOrdinals()
    {
        using var h = new AssertionHarness();
        var rule = new NetEvolve.Schemas.Schematron.Scopes.RuleScope(Rule(id: null), [], h.Rule.Scope);
        var assertion = MakeAssert("false", Message("m"));
        var position = new AssertionPosition(1, 2, 3);

        var ok = AssertionCompiler.TryCompile(
            assertion,
            rule,
            h.Schema,
            h.Binding,
            in position,
            h.Problems,
            out var compiled
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(compiled.Constraint).IsEqualTo("#p1.r2.a3");
        await Assert.That(compiled.RuleId).IsEqualTo(string.Empty);
        await Assert.That(compiled.Id).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RoleFlagsAndSeverity_AreLeftToTheAnnotationPackage()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("false", Message("m")));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.Role).IsEqualTo(string.Empty);
        await Assert.That(finding.Flags).IsEqualTo(string.Empty);
        await Assert.That(finding.Severity).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Finding_IsStillDelivered_WhenTheIssueLimitDroppedTheIssue()
    {
        using var h = new AssertionHarness(maxIssues: 1);
        var assertion = h.Compile(MakeAssert("false", Message("m")));

        _ = h.Evaluate(assertion);
        _ = h.Evaluate(assertion);

        await Assert.That(h.Sink.Findings.Count).IsEqualTo(2);
        await Assert.That(h.Sink.Findings[0].Issue).IsNotNull();
        await Assert.That(h.Sink.Findings[1].Issue).IsNull();
        await Assert.That(h.Mapper.HasDroppedError).IsTrue();
    }

    [Test]
    public async Task Finding_AtTheContextNode_CarriesTheLocationPath()
    {
        using var h = new AssertionHarness();
        var assertion = h.Compile(MakeAssert("false", Message("m")));

        _ = h.Evaluate(assertion, "order2");

        await Assert
            .That(h.Sink.Findings[0].Location)
            .IsEqualTo(
                "/*[local-name()='root' and namespace-uri()=''][1]/*[local-name()='order' and namespace-uri()=''][2]"
            );
    }

    [Test]
    public async Task Finding_ReusesTheEvaluatorForManyNodes()
    {
        using var h = new AssertionHarness();
        h.Binding.Texts["id"] = "x";
        var assertion = h.Compile(MakeAssert("false", Message(Name(), Text(":"), ValueOf("id"))));

        _ = h.Evaluate(assertion, "order1");
        _ = h.Evaluate(assertion, "px");
        _ = h.Evaluate(assertion, "order2");

        await Assert.That(h.Sink.Findings.ConvertAll(f => f.Message)).IsEquivalentTo(["order:x", "p:x:x", "order:x"]);
    }

    [Test]
    public async Task Compile_UsesTheKindOfEachExpression()
    {
        using var h = new AssertionHarness();
        _ = h.Compile(MakeAssert("t", Message(ValueOf("v"), Name("n")), subject: "s"));

        await Assert
            .That(h.Binding.Compiled.ConvertAll(c => (c.Text, c.Kind)))
            .IsEquivalentTo([
                ("t", QueryExpressionKind.Boolean),
                ("s", QueryExpressionKind.Nodes),
                ("v", QueryExpressionKind.Value),
                ("n", QueryExpressionKind.Value),
            ]);
    }

    [Test]
    public async Task Compile_InvalidTest_ReportsTheBindingProblem()
    {
        using var h = new AssertionHarness();
        var assertion = MakeAssert("invalid", Message("m"));
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    public async Task Compile_UndefinedVariable_ReportsTheBindingProblem()
    {
        using var h = new AssertionHarness();
        var assertion = MakeAssert("false", Message(ValueOf("$missing")));
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.VariableUndefined);
    }
}
