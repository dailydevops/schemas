namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions.AssertionModel;

/// <summary>Every internal boundary of the assertion package rejects null arguments.</summary>
public class AssertionGuardTests
{
    [Test]
    public async Task Evaluator_Constructor_NullArguments_Throw()
    {
        using var h = new AssertionHarness();
        var mapper = new IssueMapper(new ValidationIssueCollector(ValidationLimits.Default), null);
        var problems = new FakeProblemSink();

        await Assert
            .That(() => new AssertionEvaluator(null!, h.Binder, mapper, problems, null, 10))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new AssertionEvaluator(h.Run, null!, mapper, problems, null, 10))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new AssertionEvaluator(h.Run, h.Binder, null!, problems, null, 10))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new AssertionEvaluator(h.Run, h.Binder, mapper, null!, null, 10))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluator_Constructor_NegativeMaximum_Throws()
    {
        using var h = new AssertionHarness();
        var mapper = new IssueMapper(new ValidationIssueCollector(ValidationLimits.Default), null);

        await Assert
            .That(() => new AssertionEvaluator(h.Run, h.Binder, mapper, new FakeProblemSink(), null, -1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Evaluator_Evaluate_NullAssertion_Throws()
    {
        using var h = new AssertionHarness();
        var focus = new QueryFocus(AssertionTree.Node("order1"), 1, 1);

        await Assert.That(() => h.Evaluator.Evaluate(null!, in focus)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(0, 1, 1, 0)]
    [Arguments(1, 0, 1, 0)]
    [Arguments(1, 1, 0, 0)]
    [Arguments(-1, 1, 1, 0)]
    [Arguments(1, 1, 1, -1)]
    public async Task Position_OrdinalBelowItsMinimum_Throws(int pattern, int rule, int assertion, int expansion) =>
        await Assert
            .That(() => new AssertionPosition(pattern, rule, assertion, expansion))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Compiler_NullArguments_Throw()
    {
        using var h = new AssertionHarness();
        var assertion = MakeAssert("false", Message("m"));
        var position = new AssertionPosition(1, 1, 1);

        await Assert
            .That(() =>
                AssertionCompiler.TryCompile(null!, h.Rule, h.Schema, h.Binding, in position, h.Problems, out _)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                AssertionCompiler.TryCompile(assertion, null!, h.Schema, h.Binding, in position, h.Problems, out _)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                AssertionCompiler.TryCompile(assertion, h.Rule, null!, h.Binding, in position, h.Problems, out _)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, null!, in position, h.Problems, out _)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => AssertionCompiler.TryCompile(assertion, h.Rule, h.Schema, h.Binding, in position, null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CompiledTypes_NullArguments_Throw()
    {
        using var h = new AssertionHarness();
        var compiled = h.Compile(MakeAssert("false", Message("m")));

        await Assert.That(() => new CompiledMessage(null!, default, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledMessage("x", default, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new CompiledAssertion(false, "", "", "c", "t", default, null!, null, compiled.Message, [], []))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new CompiledAssertion(false, "", "", "c", "t", default, compiled.TestExpression, null, null!, [], [])
            )
            .Throws<ArgumentNullException>();
    }
}
