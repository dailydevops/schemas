namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Arrays;

using System.Threading;
using System.Threading.Tasks;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Arrays.ArrayFixture;

/// <summary>Sequence type matching of arrays: <c>array(*)</c>, <c>array(T)</c>, and the function test (XPath 3.1, 2.5.5.6 and 2.5.5.2).</summary>
public class ArrayTypeTests
{
    [Test]
    [Arguments("[1, 2] instance of array(*)", true)]
    [Arguments("[] instance of array(*)", true)]
    [Arguments("[1, 2] instance of array(xs:integer)", true)]
    [Arguments("[] instance of array(xs:integer)", true)]
    [Arguments("[1, 'a'] instance of array(xs:integer)", false)]
    [Arguments("[(1, 2), 3] instance of array(xs:integer)", false)]
    [Arguments("[(1, 2), 3] instance of array(xs:integer+)", true)]
    [Arguments("[(), 3] instance of array(xs:integer?)", true)]
    [Arguments("[(), 3] instance of array(xs:integer)", false)]
    [Arguments("[1, 2] instance of array(xs:decimal)", true)]
    [Arguments("[1, 2] instance of array(item()*)", true)]
    [Arguments("[[1], [2]] instance of array(array(xs:integer))", true)]
    [Arguments("[[1], ['a']] instance of array(array(xs:integer))", false)]
    [Arguments("[1, 2] instance of array(*)+", true)]
    [Arguments("([1], [2]) instance of array(*)*", true)]
    [Arguments("([1], 2) instance of array(*)*", false)]
    [Arguments("1 instance of array(*)", false)]
    [Arguments("abs#1 instance of array(*)", false)]
    [Arguments("() instance of array(*)", false)]
    public async Task InstanceOf_ArrayTests(string expression, bool expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo("boolean:" + (expected ? "True" : "False"));
    }

    [Test]
    [Arguments("[1, 2] instance of function(*)", true)]
    [Arguments("[1, 2] instance of function(xs:integer) as item()*", true)]
    [Arguments("[1, 2] instance of function(xs:string) as item()*", false)]
    [Arguments("[1, 2] instance of function(xs:integer, xs:integer) as item()*", false)]
    [Arguments("[1, 2] instance of array(*) and [1, 2] instance of function(*)", true)]
    public async Task AnArray_MatchesTheFunctionTests(
        string expression,
        bool expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run(expression)).IsEqualTo("boolean:" + (expected ? "True" : "False"));
    }

    [Test]
    public async Task ATypedParameter_AcceptsAMatchingArray_AndRejectsOthers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("(function($a as array(xs:integer)) { array:size($a) })([1, 2])")).IsEqualTo("integer:2");
        await Assert.That(Code("(function($a as array(xs:integer)) { array:size($a) })(['a'])")).IsEqualTo("XPTY0004");
        await Assert.That(Code("(function($a as array(*)) { array:size($a) })(1)")).IsEqualTo("XPTY0004");
    }
}
