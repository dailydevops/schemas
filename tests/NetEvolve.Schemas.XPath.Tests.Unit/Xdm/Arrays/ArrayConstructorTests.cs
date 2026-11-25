namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Arrays;

using System.Threading;
using System.Threading.Tasks;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays.ArrayFixture;

/// <summary>The square and the curly array constructor (XPath 3.1, 3.11.2) and the call of an array as a function item.</summary>
public class ArrayConstructorTests
{
    [Test]
    [Arguments("array:size([(1, 2)])", "integer:1")]
    [Arguments("array:size(array { (1, 2) })", "integer:2")]
    [Arguments("array:size([1, 2, 3])", "integer:3")]
    [Arguments("array:size([])", "integer:0")]
    [Arguments("array:size(array {})", "integer:0")]
    [Arguments("array:size(array { () })", "integer:0")]
    [Arguments("array:size([(), ()])", "integer:2")]
    [Arguments("array:size(array { 1 to 3 })", "integer:3")]
    [Arguments("array:size(array { [1, 2], 3 })", "integer:2")]
    [Arguments("array:flatten(array { for $i in 1 to 3 return $i * 2 })", "integer:2, integer:4, integer:6")]
    [Arguments("array:get([1, (2, 3)], 2)", "integer:2, integer:3")]
    [Arguments("array:get([1, ()], 2)", "()")]
    [Arguments("count([1, 2])", "integer:1")]
    public async Task Constructors_BuildMembers_FromExpressionsOrItems(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("[10, 20, 30](2)", "integer:20")]
    [Arguments("[10, (20, 21)](2)", "integer:20, integer:21")]
    [Arguments("let $a := [10, 20] return $a(1)", "integer:10")]
    [Arguments("[[1, 2], [3]](1)(2)", "integer:2")]
    [Arguments("function-arity([1, 2])", "integer:1")]
    [Arguments("function-name([1])", "()")]
    public async Task AnArray_IsCalledWithAOneBasedPosition(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("[10, 20](0)", "FOAY0001")]
    [Arguments("[10, 20](3)", "FOAY0001")]
    [Arguments("[](1)", "FOAY0001")]
    [Arguments("[10, 20](-1)", "FOAY0001")]
    [Arguments("[10, 20]('a')", "XPTY0004")]
    [Arguments("[10, 20](1, 2)", "XPTY0004")]
    [Arguments("[10, 20]()", "XPTY0004")]
    public async Task ACallOfAnArray_OutOfBoundsOrIllTyped_IsAnError(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }

    [Test]
    [Arguments("data([1, 2, 3])", "integer:1, integer:2, integer:3")]
    [Arguments("data([1, [2, 3], [[4]]])", "integer:1, integer:2, integer:3, integer:4")]
    [Arguments("data(([1, 2], 3, [4, [5]]))", "integer:1, integer:2, integer:3, integer:4, integer:5")]
    [Arguments("data([(1, 2), 3])", "integer:1, integer:2, integer:3")]
    [Arguments("data([])", "()")]
    [Arguments("data([[], ()])", "()")]
    [Arguments("[1, 2] = 2", "boolean:True")]
    [Arguments("[1, 2] = 3", "boolean:False")]
    [Arguments("[[1], [2]] = 2", "boolean:True")]
    [Arguments("[1] + 1", "integer:2")]
    [Arguments("abs([-1])", "integer:1")]
    [Arguments("count(abs([]))", "integer:0")]
    [Arguments("string-join(['a', 'b'], '-')", "string:a-b")]
    [Arguments("string-join(['a', ['b', 'c']], '-')", "string:a-b-c")]
    public async Task Atomization_FlattensNestedArrays(
        string expression,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("abs([1, 2])", "XPTY0004")]
    [Arguments("[1, 2] + 1", "XPTY0004")]
    [Arguments("boolean([1])", "FORG0006")]
    [Arguments("if ([]) then 1 else 2", "FORG0006")]
    public async Task AnArray_AsAnAtomicOperand_OrABoolean_FollowsTheItemRules(
        string expression,
        string code,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Code(expression)).IsEqualTo(code);
    }
}
