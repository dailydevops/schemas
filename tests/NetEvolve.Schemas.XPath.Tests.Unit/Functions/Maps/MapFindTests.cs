namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using static MapFixture;

/// <summary><c>map:find</c> (Functions and Operators 3.1, 17.1.7): the values of a key in the maps and arrays nested in a sequence.</summary>
public class MapFindTests
{
    [Test]
    [Arguments("array:size(map:find((), 'a'))", "integer:0")]
    [Arguments("array:size(map:find(map { 'a' : 1 }, 'b'))", "integer:0")]
    [Arguments("array:size(map:find(map { 'a' : 1 }, 'a'))", "integer:1")]
    [Arguments("array:flatten(map:find(map { 'a' : 1 }, 'a'))", "integer:1")]
    [Arguments("array:flatten(map:find(map { 1 : 'x' }, 1.0))", "string:x")]
    [Arguments("array:size(map:find(map { 1 : 'x' }, '1'))", "integer:0")]
    [Arguments("array:flatten(map:find((map { 'a' : 1 }, map { 'a' : 2 }), 'a'))", "integer:1, integer:2")]
    [Arguments("array:flatten(map:find([map { 'a' : 1 }, map { 'a' : 2 }], 'a'))", "integer:1, integer:2")]
    [Arguments("array:flatten(map:find([[map { 'a' : 1 }], (3, map { 'a' : 2 })], 'a'))", "integer:1, integer:2")]
    [Arguments("array:size(map:find(map { 'a' : map { 'a' : 1 } }, 'a'))", "integer:2")]
    [Arguments("array:size(map:find(map { 'a' : (map { 'a' : 1 }, map { 'a' : 2 }) }, 'a'))", "integer:3")]
    [Arguments("array:size(map:find(map { 'b' : [map { 'a' : 1 }] }, 'a'))", "integer:1")]
    [Arguments("array:size(map:find(map { 'a' : () }, 'a'))", "integer:1")]
    [Arguments("array:size(map:find((1, 'x', 2.5), 1))", "integer:0")]
    [Arguments("array:size(map:find(map { xs:double('NaN') : 1 }, xs:double('NaN')))", "integer:1")]
    public async Task Finding_ReturnsTheArrayOfValues(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("map:find(map {}, ())", "XPTY0004")]
    [Arguments("map:find(map {}, (1, 2))", "XPTY0004")]
    [Arguments("map:find(map {}, map {})", "FOTY0013")]
    public async Task Finding_WithABadKey_RaisesTheError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    public async Task Finding_BeforeXPath31_IsNotAFunction(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code("map:find((), 1)", XPathVersion.V30)).IsEqualTo("XPST0017");
    }
}
