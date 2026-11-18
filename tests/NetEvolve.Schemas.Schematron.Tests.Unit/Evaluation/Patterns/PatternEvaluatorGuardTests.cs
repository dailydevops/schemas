namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System.Threading;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>Every internal boundary of the pattern evaluation rejects null arguments.</summary>
public class PatternEvaluatorGuardTests
{
    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        await Assert
            .That(() => new PatternEvaluator(null!, h.Run, h.Binder, h.Assertions, h.Mapper, h.Problems, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PatternEvaluator(h.Matcher, null!, h.Binder, h.Assertions, h.Mapper, h.Problems, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PatternEvaluator(h.Matcher, h.Run, null!, h.Assertions, h.Mapper, h.Problems, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PatternEvaluator(h.Matcher, h.Run, h.Binder, null!, h.Mapper, h.Problems, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PatternEvaluator(h.Matcher, h.Run, h.Binder, h.Assertions, null!, h.Problems, default))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new PatternEvaluator(h.Matcher, h.Run, h.Binder, h.Assertions, h.Mapper, null!, default))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluate_NullSets_Throws()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        await Assert.That(() => _ = h.Evaluator.Evaluate(null!, h.Document)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Evaluate_DocumentWithoutATree_Throws()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));

        await Assert.That(() => _ = h.Evaluator.Evaluate(h.Sets, default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CompiledRule_NullArguments_Throw()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));
        var rule = h.Sets[0].Rules[0];

        await Assert.That(() => new CompiledRule(null!, rule.Context, "r", "a", [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledRule(rule.Scope, null!, "r", "a", [])).Throws<ArgumentNullException>();
        await Assert
            .That(() => new CompiledRule(rule.Scope, rule.Context, null!, "a", []))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new CompiledRule(rule.Scope, rule.Context, "r", null!, []))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new CompiledRule(rule.Scope, rule.Context, "r", "a", null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task CompiledRuleSet_NullArguments_Throw()
    {
        var schema = Schema(Pattern("p", Rule("r1", "a", Holds())));
        using var h = new PatternHarness(schema, E("a"));
        var scope = h.Sets[0].Scope;

        await Assert.That(() => new CompiledRuleSet(null!, "p", "", "", false, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledRuleSet(scope, null!, "", "", false, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledRuleSet(scope, "p", null!, "", false, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledRuleSet(scope, "p", "", null!, false, [])).Throws<ArgumentNullException>();
        await Assert.That(() => new CompiledRuleSet(scope, "p", "", "", false, null!)).Throws<ArgumentNullException>();
    }
}
