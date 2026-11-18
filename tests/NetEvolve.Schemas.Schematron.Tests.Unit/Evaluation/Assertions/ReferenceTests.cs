namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>Referenced diagnostics and properties (design #150 EP-7; unknown ids are NES3012, #137 AP-9).</summary>
public class ReferenceTests
{
    [Test]
    public async Task Diagnostics_AreAttachedWithTheirRenderedText()
    {
        using var h = new AssertionHarness(
            diagnostics:
            [
                Diagnostic("d1", Message("first")),
                Diagnostic("d2", Message(Text("second "), ValueOf("id"))),
                Diagnostic("unused", Message("never")),
            ]
        );
        h.Binding.Texts["id"] = "7";
        var assertion = h.Compile(MakeAssert("false", Message("m"), diagnostics: ["d1", "d2"]));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.Diagnostics).IsEquivalentTo([("d1", "first"), ("d2", "second 7")]);
        await Assert.That(finding.Properties.Count).IsEqualTo(0);
        await Assert.That(finding.Message).IsEqualTo("m");
    }

    [Test]
    public async Task Properties_AreAttachedWithTheirRenderedText()
    {
        using var h = new AssertionHarness(
            properties: [Property("p1", Message(Text("p="), Name())), Property("p2", Message("two"))]
        );
        var assertion = h.Compile(MakeReport("true", Message("m"), properties: ["p2", "p1"]));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.Properties).IsEquivalentTo([("p2", "two"), ("p1", "p=order")]);
        await Assert.That(finding.Diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Finding_OrderIs_Test_Subject_Message_Diagnostics_Properties()
    {
        using var h = new AssertionHarness(
            diagnostics: [Diagnostic("d", Message(ValueOf("fromDiagnostic")))],
            properties: [Property("p", Message(ValueOf("fromProperty")))]
        );
        var assertion = h.Compile(
            MakeAssert("false", Message(ValueOf("fromMessage")), subject: "subj", diagnostics: ["d"], properties: ["p"])
        );

        _ = h.Evaluate(assertion);

        await Assert
            .That(h.Binding.Evaluated)
            .IsEquivalentTo(["false", "subj", "fromMessage", "fromDiagnostic", "fromProperty"]);
    }

    [Test]
    public async Task Finding_PartsAreEvaluatedOnlyWhenTheAssertionFires()
    {
        using var h = new AssertionHarness(
            diagnostics: [Diagnostic("d", Message(ValueOf("fromDiagnostic")))],
            properties: [Property("p", Message(ValueOf("fromProperty")))]
        );
        var assertion = h.Compile(
            MakeAssert("true", Message(ValueOf("fromMessage")), subject: "subj", diagnostics: ["d"], properties: ["p"])
        );

        var outcome = h.Evaluate(assertion);

        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Silent);
        await Assert.That(h.Binding.Evaluated).IsEquivalentTo(["true"]);
        await Assert.That(h.Binder.Bound.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Diagnostic_Error_IsDroppedFromTheReferences_KeepsTheFinding()
    {
        using var h = new AssertionHarness(
            diagnostics: [Diagnostic("d1", Message(Text("x"), ValueOf("bad"))), Diagnostic("d2", Message("ok"))]
        );
        var assertion = h.Compile(MakeAssert("false", Message("m"), diagnostics: ["d1", "d2"]));

        var outcome = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(outcome).IsEqualTo(AssertionOutcome.Fired);
        await Assert.That(finding.Diagnostics).IsEquivalentTo([("d2", "ok")]);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Property_Error_IsDroppedFromTheReferences_KeepsTheFinding()
    {
        using var h = new AssertionHarness(
            properties: [Property("p1", Message(ValueOf("bad"))), Property("p2", Message("ok"))]
        );
        var assertion = h.Compile(MakeReport("true", Message("m"), properties: ["p1", "p2"]));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.Properties).IsEquivalentTo([("p2", "ok")]);
        await Assert.That(finding.Message).IsEqualTo("m");
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Diagnostic_MessageIsCompiledInTheScopeOfTheRule()
    {
        using var h = new AssertionHarness(diagnostics: [Diagnostic("d", Message(ValueOf("$x")))], variables: ["x"]);
        h.Binding.Texts["$x"] = "X";
        var assertion = h.Compile(MakeAssert("false", Message("m"), diagnostics: ["d"]));

        _ = h.Evaluate(assertion);

        await Assert.That(h.Binding.Compiled.TrueForAll(c => ReferenceEquals(c.Scope, h.Rule.Scope))).IsTrue();
        await Assert.That(h.Binder.Bound).IsEquivalentTo([0]);
        await Assert.That(h.Sink.Findings[0].Diagnostics).IsEquivalentTo([("d", "X")]);
    }

    [Test]
    public async Task UnknownDiagnosticId_IsReportedAsUnresolvedReference()
    {
        using var h = new AssertionHarness(diagnostics: [Diagnostic("d1", Message("x"))]);
        var assertion = MakeAssert("false", Message("m"), diagnostics: ["d1", "nope"]);
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
        await Assert.That(h.Problems.Problems[0].Message).Contains("nope");
        await Assert.That(h.Problems.Problems[0].Location).IsEqualTo(assertion.Location);
    }

    [Test]
    public async Task UnknownPropertyId_IsReportedAsUnresolvedReference()
    {
        using var h = new AssertionHarness(properties: [Property("p1", Message("x"))]);
        var assertion = MakeReport("true", Message("m"), properties: ["missing"]);
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(1);
        await Assert.That(h.Problems.Problems[0].Code).IsEqualTo(SchematronDiagnosticCodes.IdrefUnresolved);
        await Assert.That(h.Problems.Problems[0].Message).Contains("missing");
    }

    [Test]
    public async Task ReferenceWithoutAnyDeclaration_IsReportedAsUnresolvedReference()
    {
        using var h = new AssertionHarness();
        var assertion = MakeAssert("false", Message("m"), diagnostics: ["d"], properties: ["p"]);
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Compile_ReportsEveryProblem_NotJustTheFirst()
    {
        using var h = new AssertionHarness(diagnostics: [Diagnostic("d", Message(ValueOf("invalid-d")))]);
        var assertion = MakeAssert(
            "invalid-test",
            Message(ValueOf("invalid-m")),
            subject: "invalid-s",
            diagnostics: ["d", "nope"]
        );
        var position = new AssertionPosition(1, 1, 1);

        var ok = AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _);

        await Assert.That(ok).IsFalse();
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(5);
    }

    [Test]
    public async Task ReferenceBeyondTheMaximum_IsDropped_WhileTheMessageIsCutAndKept()
    {
        using var h = new AssertionHarness(
            diagnostics: [Diagnostic("d", Message("xyzxyz"))],
            properties: [Property("p", Message("pq"))],
            maxTextLength: 3
        );
        var assertion = h.Compile(MakeAssert("false", Message("abcdef"), diagnostics: ["d"], properties: ["p"]));

        _ = h.Evaluate(assertion);

        var finding = h.Sink.Findings[0];
        await Assert.That(finding.Message).IsEqualTo("abc");
        await Assert.That(finding.Diagnostics.Count).IsEqualTo(0);
        await Assert.That(finding.Properties).IsEquivalentTo([("p", "pq")]);
        await Assert.That(h.Problems.Problems.Count).IsEqualTo(2);
    }
}
