namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using static ArrayFixture;

/// <summary>The functions of the <c>array</c> namespace (Functions and Operators 3.1, 17.3).</summary>
public class ArrayFunctionTests
{
    [Test]
    [Arguments("array:size([])", "integer:0")]
    [Arguments("array:size([1, (2, 3)])", "integer:2")]
    [Arguments("array:get([1, 2, 3], 3)", "integer:3")]
    [Arguments("array:get([1, (2, 3)], 2)", "integer:2, integer:3")]
    [Arguments("array:flatten(array:put([1, 2, 3], 2, 'x'))", "integer:1, string:x, integer:3")]
    [Arguments("array:flatten(array:put([1], 1, (7, 8)))", "integer:7, integer:8")]
    [Arguments("array:size(array:put([1], 1, ()))", "integer:1")]
    [Arguments("array:flatten(array:append([1, 2], 3))", "integer:1, integer:2, integer:3")]
    [Arguments("array:size(array:append([1], (2, 3)))", "integer:2")]
    [Arguments("array:size(array:append([], ()))", "integer:1")]
    [Arguments("array:flatten(array:subarray([1, 2, 3, 4], 2))", "integer:2, integer:3, integer:4")]
    [Arguments("array:flatten(array:subarray([1, 2, 3, 4], 2, 2))", "integer:2, integer:3")]
    [Arguments("array:flatten(array:subarray([1, 2, 3], 1, 3))", "integer:1, integer:2, integer:3")]
    [Arguments("array:size(array:subarray([1, 2, 3], 4))", "integer:0")]
    [Arguments("array:size(array:subarray([1, 2, 3], 4, 0))", "integer:0")]
    [Arguments("array:size(array:subarray([], 1))", "integer:0")]
    [Arguments("array:flatten(array:remove([1, 2, 3], 2))", "integer:1, integer:3")]
    [Arguments("array:flatten(array:remove([1, 2, 3], (1, 3)))", "integer:2")]
    [Arguments("array:flatten(array:remove([1, 2, 3], (2, 2)))", "integer:1, integer:3")]
    [Arguments("array:size(array:remove([1, 2, 3], ()))", "integer:3")]
    [Arguments("array:flatten(array:insert-before([1, 2, 3], 2, 'x'))", "integer:1, string:x, integer:2, integer:3")]
    [Arguments("array:flatten(array:insert-before([1, 2, 3], 4, 'x'))", "integer:1, integer:2, integer:3, string:x")]
    [Arguments("array:flatten(array:insert-before([1, 2], 1, (8, 9)))", "integer:8, integer:9, integer:1, integer:2")]
    [Arguments("array:size(array:insert-before([1, 2], 1, (8, 9)))", "integer:3")]
    [Arguments("array:head([1, 2, 3])", "integer:1")]
    [Arguments("array:head([(1, 2), 3])", "integer:1, integer:2")]
    [Arguments("array:flatten(array:tail([1, 2, 3]))", "integer:2, integer:3")]
    [Arguments("array:size(array:tail([1]))", "integer:0")]
    [Arguments("array:flatten(array:reverse([1, (2, 3), 4]))", "integer:4, integer:2, integer:3, integer:1")]
    [Arguments("array:size(array:reverse([]))", "integer:0")]
    [Arguments("array:flatten(array:join(([1, 2], [3], [])))", "integer:1, integer:2, integer:3")]
    [Arguments("array:size(array:join(()))", "integer:0")]
    [Arguments("array:size(array:join(([1, (2, 3)], [4])))", "integer:3")]
    public async Task TheStructuralFunctions_ReturnTheSpecifiedArray(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("array:flatten(())", "()")]
    [Arguments("array:flatten('a')", "string:a")]
    [Arguments("array:flatten([])", "()")]
    [Arguments("array:flatten([(), 1])", "integer:1")]
    [Arguments("array:flatten([1, [2, 3], [[4]]])", "integer:1, integer:2, integer:3, integer:4")]
    [Arguments("array:flatten(([1, 2], 3, [4, [5]]))", "integer:1, integer:2, integer:3, integer:4, integer:5")]
    [Arguments("array:flatten((1, ([2], (3, [4, [()]]))))", "integer:1, integer:2, integer:3, integer:4")]
    public async Task Flatten_HandlesArraysInsideSequences(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("array:get([1, 2, 3], 0)", "FOAY0001")]
    [Arguments("array:get([1, 2, 3], 4)", "FOAY0001")]
    [Arguments("array:get([1, 2, 3], -1)", "FOAY0001")]
    [Arguments("array:get([], 1)", "FOAY0001")]
    [Arguments("array:put([1, 2, 3], 0, 'x')", "FOAY0001")]
    [Arguments("array:put([1, 2, 3], 4, 'x')", "FOAY0001")]
    [Arguments("array:put([], 1, 'x')", "FOAY0001")]
    [Arguments("array:remove([1, 2, 3], 0)", "FOAY0001")]
    [Arguments("array:remove([1, 2, 3], 4)", "FOAY0001")]
    [Arguments("array:remove([1, 2, 3], (1, 4))", "FOAY0001")]
    [Arguments("array:insert-before([1, 2, 3], 0, 'x')", "FOAY0001")]
    [Arguments("array:insert-before([1, 2, 3], 5, 'x')", "FOAY0001")]
    [Arguments("array:subarray([1, 2, 3], 0)", "FOAY0001")]
    [Arguments("array:subarray([1, 2, 3], 5)", "FOAY0001")]
    [Arguments("array:subarray([1, 2, 3], 2, 3)", "FOAY0001")]
    [Arguments("array:subarray([1, 2, 3], 4, 1)", "FOAY0001")]
    [Arguments("array:subarray([1, 2, 3], 1, -1)", "FOAY0002")]
    [Arguments("array:subarray([1, 2, 3], 5, -1)", "FOAY0002")]
    [Arguments("array:head([])", "FOAY0001")]
    [Arguments("array:tail([])", "FOAY0001")]
    public async Task APositionOutOfBounds_OrANegativeLength_IsAnError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    [Arguments("array:size(1)", "XPTY0004")]
    [Arguments("array:size(())", "XPTY0004")]
    [Arguments("array:size((1, 2))", "XPTY0004")]
    [Arguments("array:size(abs#1)", "XPTY0004")]
    [Arguments("array:get([1], 'a')", "XPTY0004")]
    [Arguments("array:join(1)", "XPTY0004")]
    [Arguments("array:join(([1], 2))", "XPTY0004")]
    public async Task AnArgumentThatIsNoArray_IsXPTY0004(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    public async Task TheSourceArray_IsUnchangedAfterPutAndAppend(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(
                Run(
                    "let $a := [1, 2, 3], $b := array:put($a, 1, 9) return (array:get($a, 1), array:get($b, 1), array:size($a))"
                )
            )
            .IsEqualTo("integer:1, integer:9, integer:3");
        await Assert
            .That(
                Run(
                    "let $a := [1, 2], $b := array:append($a, 3) return (array:size($a), array:size($b), array:flatten($a))"
                )
            )
            .IsEqualTo("integer:2, integer:3, integer:1, integer:2");
        await Assert
            .That(
                Run(
                    "let $a := [1, 2, 3, 4], $b := array:subarray($a, 2, 2), $c := array:put($b, 1, 'x') return (array:flatten($a), array:flatten($c))"
                )
            )
            .IsEqualTo("integer:1, integer:2, integer:3, integer:4, string:x, integer:3");
        await Assert
            .That(
                Run(
                    "let $a := [1, 2, 3], $b := array:tail($a), $c := array:append($b, 4) return (array:flatten($a), array:flatten($c))"
                )
            )
            .IsEqualTo("integer:1, integer:2, integer:3, integer:2, integer:3, integer:4");
    }

    [Test]
    [Arguments("array:size(())", "XPST0017")]
    public async Task TheFunctions_AreUnknownBeforeXPath31(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression, XPathVersion.V30)).IsEqualTo(code);
    }
}
