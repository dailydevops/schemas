namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Dispatch;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Evaluation.Dispatch;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>All active patterns are matched in one traversal with a rule index, with the fired rules and their order of naive matching (SCH-72).</summary>
public class SinglePassDispatchTests
{
    private static readonly string[] Contexts =
    [
        "a",
        "b",
        "c",
        "*",
        "@x",
        "@y",
        "@*",
        "text()",
        "/",
        "node()",
    ];

    [Test]
    public async Task Traversals_TenActivePatterns_OnePass()
    {
        var patterns = new SchematronPatternBase[10];
        for (var i = 0; i < patterns.Length; i++)
        {
            patterns[i] = Pattern("p" + i, Rule("r" + i, "a", Fails("m" + i)));
        }

        using var h = new PatternHarness(Schema(patterns), E("r", E("a"), E("b", E("a"))));

        _ = h.Evaluate();

        await Assert.That(h.Evaluator.Traversals).IsEqualTo(1);
        await Assert.That(h.Log.Rules.Count).IsEqualTo(20);
    }

    [Test]
    public async Task Traversals_PerPatternPasses_AreOneEach()
    {
        var schema = Schema(Pattern("p1", Rule("r1", "a")), Pattern("p2", Rule("r2", "b")));
        using var h = new PatternHarness(schema, E("r", E("a")));

        foreach (var set in h.Sets)
        {
            _ = h.Evaluator.Evaluate([set], h.Document);
        }

        await Assert.That(h.Evaluator.Traversals).IsEqualTo(2);
    }

    [Test]
    public async Task Traversals_SinglePattern_OnePass()
    {
        using var h = new PatternHarness(Schema(Pattern("p", Rule("r", "a"))), E("r", E("a")));

        _ = h.Evaluate();

        await Assert.That(h.Evaluator.Traversals).IsEqualTo(1);
    }

    [Test]
    public async Task Dispatch_WildcardAndNonElementContexts_AreStillMatched()
    {
        var schema = Schema(
            Pattern("p1", Rule("r1", "*", Fails("e"))),
            Pattern("p2", Rule("r2", "@*", Fails("a"))),
            Group("g3", Rule("r3", "text()", Fails("t")), Rule("r4", "/", Fails("d"))),
            Pattern("p5", Rule("r5", "node()", Fails("n")))
        );
        using var h = new PatternHarness(schema, E("r", A("x", "1"), E("a"), "t"));

        _ = h.Evaluate();

        await Assert.That(h.Log.Fired).IsEqualTo("r1,r1,r2,r4,r3,r5,r5,r5");
        await Assert.That(h.Evaluator.Traversals).IsEqualTo(1);
    }

    [Test]
    public async Task Dispatch_NamedContexts_AreOnlyAskedForTheirCandidates()
    {
        var schema = Schema(
            Pattern("p1", Rule("r1", "a")),
            Pattern("p2", Rule("r2", "b")),
            Pattern("p3", Rule("r3", "@x"))
        );
        using var h = new PatternHarness(schema, E("r", E("a", A("x", "1")), E("a"), E("b"), E("c")));
        var evaluator = NamedContexts.Evaluator(h, out var sets);

        _ = evaluator.Evaluate(sets, h.Document);

        // Two a elements, one b element, and one x attribute: nothing else is a candidate of a context.
        await Assert.That(h.Matcher.MatchCalls).IsEqualTo(4);
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r1,r2,r3");
        await Assert.That(evaluator.Traversals).IsEqualTo(1);
    }

    [Test]
    public async Task Dispatch_NameTestAndFallbackInOnePattern_UnionOfBoth()
    {
        var schema = Schema(Group("g", Rule("r1", "a"), Rule("r2", "*")), Pattern("p", Rule("r3", "c")));
        using var h = new PatternHarness(schema, E("r", E("a"), E("b"), E("c")));
        var evaluator = NamedContexts.Evaluator(h, out var sets);

        _ = evaluator.Evaluate(sets, h.Document);

        await Assert.That(h.Log.Fired).IsEqualTo("r2,r1,r2,r2,r2,r3");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    [Arguments(10)]
    [Arguments(11)]
    [Arguments(12)]
    [Arguments(13)]
    [Arguments(14)]
    [Arguments(15)]
    [Arguments(16)]
    [Arguments(17)]
    [Arguments(18)]
    [Arguments(19)]
    [Arguments(20)]
    public async Task Dispatch_GeneratedSchemasAndDocuments_FireTheSameRulesInTheSameOrderAsNaiveMatching(int seed)
    {
        var random = new Random(seed);
        var schema = GenerateSchema(random);
        var document = GenerateElement(random, 0);

        using var naive = new PatternHarness(schema, document);
        foreach (var set in naive.Sets)
        {
            _ = naive.Evaluator.Evaluate([set], naive.Document);
        }

        using var indexed = new PatternHarness(schema, document);
        _ = indexed.Evaluate();

        using var named = new PatternHarness(schema, document);
        var evaluator = NamedContexts.Evaluator(named, out var sets);
        _ = evaluator.Evaluate(sets, named.Document);

        await Assert.That(indexed.Log.Sequence).IsEqualTo(naive.Log.Sequence);
        await Assert.That(named.Log.Sequence).IsEqualTo(naive.Log.Sequence);
        await Assert.That(indexed.Evaluator.Traversals).IsEqualTo(1);
        await Assert.That(naive.Evaluator.Traversals).IsEqualTo(naive.Sets.Count);
    }

    [Test]
    public async Task Cancellation_BeforeTheTraversal_Throws()
    {
        using var source = new CancellationTokenSource();
        var schema = Schema(Pattern("p1", Rule("r1", "a")), Pattern("p2", Rule("r2", "b")));
        using var h = new PatternHarness(schema, E("r", E("a")), cancellationToken: source.Token);
        await source.CancelAsync();

        await Assert.That(() => _ = h.Evaluate()).Throws<OperationCanceledException>();
        await Assert.That(h.Log.Events).IsEmpty();
    }

    [Test]
    public async Task Cancellation_DuringTheTraversal_Throws()
    {
        using var source = new CancellationTokenSource();
        var schema = Schema(Pattern("p1", Rule("r1", "a")), Pattern("p2", Rule("r2", "b")));
        using var h = new PatternHarness(schema, E("r", E("a")));
        var tree = BuildChain(600);
        await source.CancelAsync();

        await Assert.That(() => DispatchIndex.Build(h.Sets, tree.Node, source.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Build_NullSets_Throws()
    {
        var tree = BuildChain(1);

        await Assert.That(() => DispatchIndex.Build(null!, tree.Node, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Build_EmptyRoot_Throws()
    {
        using var h = new PatternHarness(Schema(Pattern("p", Rule("r", "a"))), E("r"));

        await Assert.That(() => DispatchIndex.Build(h.Sets, default, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Build_NoCandidate_IsEmpty()
    {
        var schema = Schema(Pattern("p1", Rule("r1", "a")), Pattern("p2", Rule("r2", "b")));
        using var h = new PatternHarness(schema, E("r", E("c")));
        _ = NamedContexts.Evaluator(h, out var sets);

        var index = DispatchIndex.Build(sets, h.Document.Node, CancellationToken.None);

        await Assert.That(index.Count).IsEqualTo(0);
    }

    private static SchematronSchema GenerateSchema(Random random)
    {
        var patterns = new SchematronPatternBase[random.Next(2, 7)];
        for (var p = 0; p < patterns.Length; p++)
        {
            var rules = new SchematronRule[random.Next(1, 4)];
            for (var r = 0; r < rules.Length; r++)
            {
                var context = Contexts[random.Next(Contexts.Length)];
                rules[r] = Rule($"p{p}r{r}", context, Holds());
            }

            patterns[p] = random.Next(3) == 0 ? Group("p" + p, rules) : Pattern("p" + p, rules);
        }

        return Schema(patterns);
    }

    private static PatternElement GenerateElement(Random random, int depth)
    {
        string[] names = ["a", "b", "c", "d"];
        var content = new List<object>();
        if (random.Next(2) == 0)
        {
            content.Add(A("x", "1"));
        }

        if (random.Next(3) == 0)
        {
            content.Add(A("y", "2"));
        }

        var children = depth >= 3 ? 0 : random.Next(0, 4);
        for (var i = 0; i < children; i++)
        {
            content.Add(random.Next(4) == 0 ? "t" : GenerateElement(random, depth + 1));
        }

        return E(names[random.Next(names.Length)], [.. content]);
    }
}
