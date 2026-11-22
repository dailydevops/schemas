namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Regex;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;

/// <summary>
/// Pins decision D2 of #266: a pattern is compiled once per pattern string instance and flag set, so a literal pattern of a compiled
/// expression is compiled once across its evaluations. Each test builds its own cache, so the counts do not depend on other tests.
/// </summary>
public class PatternCacheTests
{
    private static XPathStaticContext Statics(PatternCache cache) =>
        ApiFixture
            .Builder()
            .WithLibrary(
                new XPathFunctionLibrary(
                    "cache-test",
                    [new MatchesFunction(cache), new ReplaceFunction(cache), new TokenizeFunction(cache)]
                )
            )
            .Build();

    [Test]
    [Arguments("matches('xabcx', 'b+c')", "true")]
    [Arguments("matches('xabcx', 'B+C', 'i')", "true")]
    [Arguments("replace('xabcx', 'b+c', '-')", "xa-x")]
    [Arguments("tokenize('xabcx', 'b+c')", "xa|x")]
    public async Task ALiteralPattern_IsCompiledOnceAcrossTheEvaluationsOfOneCompiledExpression(
        string expression,
        string expected
    )
    {
        var cache = new PatternCache();
        var compiled = ApiFixture.Compile(expression, Statics(cache));
        using var context = new XPathEvaluationContext();

        for (var i = 0; i < 50; i++)
        {
            await Assert.That(ApiFixture.Render(compiled.Evaluate(context))).IsEqualTo(expected);
        }

        await Assert.That(cache.Compilations).IsEqualTo(1);
    }

    [Test]
    public async Task TheSameInstance_GivesTheSameOperations()
    {
        var cache = new PatternCache();
        var pattern = new string(['a', '+']);

        var first = cache.Get(pattern, string.Empty);
        var second = cache.Get(pattern, string.Empty);

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(cache.Compilations).IsEqualTo(1);
    }

    [Test]
    public async Task EachSetOfFlags_IsCompiledOnce_AndEquivalentFlagStringsShareTheOperations()
    {
        var cache = new PatternCache();
        var pattern = new string(['a', '+']);

        var plain = cache.Get(pattern, string.Empty);
        var ignoreCase = cache.Get(pattern, "i");
        var again = cache.Get(pattern, "ii");
        var reordered = cache.Get(pattern, "si");
        var other = cache.Get(pattern, "is");

        await Assert.That(ignoreCase).IsNotSameReferenceAs(plain);
        await Assert.That(again).IsSameReferenceAs(ignoreCase);
        await Assert.That(other).IsSameReferenceAs(reordered);
        await Assert.That(cache.Compilations).IsEqualTo(3);
    }

    [Test]
    public async Task AnEqualPatternOfAnotherInstance_IsCompiledAgain()
    {
        var cache = new PatternCache();

        _ = cache.Get(new string(['a', '+']), string.Empty);
        _ = cache.Get(new string(['a', '+']), string.Empty);

        await Assert.That(cache.Compilations).IsEqualTo(2);
    }

    [Test]
    public async Task AnInvalidPattern_IsNotCached_AndRaisesTheSameErrorEveryTime()
    {
        var cache = new PatternCache();
        var pattern = new string(['(']);

        var first = Raised(() => cache.Get(pattern, string.Empty));
        var second = Raised(() => cache.Get(pattern, string.Empty));

        await Assert.That(first).IsEqualTo("FORX0002");
        await Assert.That(second).IsEqualTo("FORX0002");
        await Assert.That(cache.Compilations).IsEqualTo(0);
    }

    [Test]
    public async Task ACacheHit_AllocatesNothing()
    {
        var cache = new PatternCache();
        var pattern = new string(['a', '+']);
        _ = cache.Get(pattern, "i");

        var bytes = AllocationAssert.Measure(() => _ = cache.Get(pattern, "i"), warmups: 20);

        await Assert.That(bytes).IsEqualTo(0L);
    }

    private static string Raised(System.Action action)
    {
        try
        {
            action();
            return string.Empty;
        }
        catch (NetEvolve.Schemas.XPath.Errors.XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }
}
