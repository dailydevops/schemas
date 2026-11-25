namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System.Threading;
using System.Threading.Tasks;
using static MapFixture;

/// <summary><c>map:merge</c> and the <c>duplicates</c> option (Functions and Operators 3.1, 17.1.1).</summary>
public class MapMergeTests
{
    private const string Pair = "(map { 1 : 'a', 2 : 'x' }, map { 1 : 'b', 3 : 'y' })";

    [Test]
    [Arguments("map:size(map:merge(()))", "integer:0")]
    [Arguments("map:size(map:merge(map {}))", "integer:0")]
    [Arguments("map:size(map:merge((map { 1 : 'a' }, map { 2 : 'b' })))", "integer:2")]
    [Arguments("map:size(map:merge((map { 1 : 'a' }, map { 2 : 'b' }, map { 3 : 'c' })))", "integer:3")]
    [Arguments("map:merge(map { 1 : 'a' })(1)", "string:a")]
    [Arguments("map:size(map:merge(" + Pair + "))", "integer:3")]
    [Arguments("map:merge(" + Pair + ")(1)", "string:a")]
    [Arguments("map:merge(" + Pair + ", map {})(1)", "string:a")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'use-first' })(1)", "string:a")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'use-last' })(1)", "string:b")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'combine' })(1)", "string:a, string:b")]
    [Arguments("count(map:merge(" + Pair + ", map { 'duplicates' : 'use-any' })(1))", "integer:1")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'use-last' })(2)", "string:x")]
    [Arguments("map:size(map:merge(" + Pair + ", map { 'duplicates' : 'combine' }))", "integer:3")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : xs:untypedAtomic('use-last') })(1)", "string:b")]
    [Arguments("map:merge(" + Pair + ", map { 'unknown' : 'whatever' })(1)", "string:a")]
    [Arguments("map:merge((map { 1 : 'a' }, map { 1.0 : 'b' }), map { 'duplicates' : 'use-last' })(1)", "string:b")]
    [Arguments(
        "map:merge((map { 1 : (1, 2) }, map { 1 : 3 }), map { 'duplicates' : 'combine' })(1)",
        "integer:1, integer:2, integer:3"
    )]
    [Arguments(
        "map:merge((map { 1 : 'a' }, map { 1 : 'b' }, map { 1 : 'c' }), map { 'duplicates' : 'combine' })(1)",
        "string:a, string:b, string:c"
    )]
    [Arguments(
        "map:merge((map { 1 : 'a' }, map { 1 : 'b' }, map { 1 : 'c' }), map { 'duplicates' : 'use-last' })(1)",
        "string:c"
    )]
    public async Task Merging_ReturnsTheMap(string expression, string expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'reject' })", "FOJS0003")]
    [Arguments("map:merge((map { 1 : 'a' }, map { 1.0 : 'b' }), map { 'duplicates' : 'reject' })", "FOJS0003")]
    [Arguments("map:merge((map { 1 : 'a' }, map { 2 : 'b' }), map { 'duplicates' : 'reject' })", "")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 'bogus' })", "FOJS0005")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : '' })", "FOJS0005")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : 1 })", "XPTY0004")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : ('use-first', 'use-last') })", "XPTY0004")]
    [Arguments("map:merge(" + Pair + ", map { 'duplicates' : () })", "XPTY0004")]
    [Arguments("map:merge(" + Pair + ", 1)", "XPTY0004")]
    [Arguments("map:merge(" + Pair + ", (map {}, map {}))", "XPTY0004")]
    [Arguments("map:merge((map {}, 1))", "XPTY0004")]
    [Arguments("map:merge(1)", "XPTY0004")]
    [Arguments("map:merge((map {}, function($a) { $a }))", "XPTY0004")]
    public async Task Merging_WithABadInput_RaisesTheError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }
}
