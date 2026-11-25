namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Maps;

using System.Threading;
using System.Threading.Tasks;
using static MapFixture;

/// <summary>The key equality of maps, <c>op:same-key</c> (Functions and Operators 3.1, 17.1.1).</summary>
public class MapSameKeyTests
{
    [Test]
    [Arguments("1", "1.0", "boolean:True")]
    [Arguments("1", "1e0", "boolean:True")]
    [Arguments("1.0", "1e0", "boolean:True")]
    [Arguments("1", "xs:float('1')", "boolean:True")]
    [Arguments("xs:integer(1)", "xs:byte(1)", "boolean:True")]
    [Arguments("0e0", "-0e0", "boolean:True")]
    [Arguments("1", "2", "boolean:False")]
    [Arguments("1", "'1'", "boolean:False")]
    [Arguments("1", "xs:untypedAtomic('1')", "boolean:False")]
    [Arguments("1", "true()", "boolean:False")]
    [Arguments("xs:double('NaN')", "xs:double('NaN')", "boolean:True")]
    [Arguments("xs:double('NaN')", "xs:float('NaN')", "boolean:True")]
    [Arguments("xs:double('NaN')", "1", "boolean:False")]
    [Arguments("xs:double('INF')", "xs:float('INF')", "boolean:True")]
    [Arguments("xs:double('INF')", "xs:double('-INF')", "boolean:False")]
    [Arguments("'a'", "'a'", "boolean:True")]
    [Arguments("'a'", "'A'", "boolean:False")]
    [Arguments("'a'", "xs:untypedAtomic('a')", "boolean:True")]
    [Arguments("'a'", "xs:anyURI('a')", "boolean:True")]
    [Arguments("xs:untypedAtomic('a')", "xs:anyURI('a')", "boolean:True")]
    [Arguments("true()", "true()", "boolean:True")]
    [Arguments("true()", "false()", "boolean:False")]
    [Arguments("xs:date('2000-01-01Z')", "xs:date('2000-01-01Z')", "boolean:True")]
    [Arguments("xs:date('2000-01-01Z')", "xs:date('2000-01-01+00:00')", "boolean:True")]
    [Arguments("xs:date('2000-01-01Z')", "xs:date('2000-01-01')", "boolean:False")]
    [Arguments("xs:date('2000-01-01')", "xs:date('2000-01-01')", "boolean:True")]
    [Arguments("xs:date('2000-01-01')", "xs:date('2000-01-02')", "boolean:False")]
    [Arguments("xs:time('10:00:00')", "xs:time('10:00:00Z')", "boolean:False")]
    [Arguments("xs:time('10:00:00Z')", "xs:time('11:00:00+01:00')", "boolean:True")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00Z')", "xs:dateTime('2000-01-01T01:00:00+01:00')", "boolean:True")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00')", "xs:dateTime('2000-01-01T00:00:00Z')", "boolean:False")]
    [Arguments("xs:dateTime('2000-01-01T00:00:00.0')", "xs:dateTime('2000-01-01T00:00:00')", "boolean:True")]
    [Arguments("xs:date('2000-01-01')", "xs:dateTime('2000-01-01T00:00:00')", "boolean:False")]
    [Arguments("xs:gYear('2000')", "xs:gYear('2000')", "boolean:True")]
    [Arguments("xs:gYear('2000')", "xs:gYear('2000Z')", "boolean:False")]
    [Arguments("xs:gYear('2000')", "xs:gYear('2001')", "boolean:False")]
    [Arguments("xs:duration('P1Y')", "xs:yearMonthDuration('P12M')", "boolean:True")]
    [Arguments("xs:duration('PT60S')", "xs:dayTimeDuration('PT1M')", "boolean:True")]
    [Arguments("xs:duration('P1Y')", "xs:duration('P1M')", "boolean:False")]
    [Arguments("xs:dayTimeDuration('PT1S')", "xs:dayTimeDuration('PT2S')", "boolean:False")]
    [Arguments("xs:hexBinary('0A')", "xs:hexBinary('0a')", "boolean:True")]
    [Arguments("xs:hexBinary('0A')", "xs:hexBinary('0B')", "boolean:False")]
    [Arguments("xs:hexBinary('0A')", "xs:base64Binary('Cg==')", "boolean:False")]
    [Arguments("xs:base64Binary('Cg==')", "xs:base64Binary('Cg==')", "boolean:True")]
    [Arguments("xs:QName('a')", "xs:QName('a')", "boolean:True")]
    [Arguments("xs:QName('a')", "xs:QName('b')", "boolean:False")]
    [Arguments("xs:QName('a')", "'a'", "boolean:False")]
    public async Task TheKeys_AreTheSameKeyOrNot(
        string left,
        string right,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The same answer through the lookup, the contains function, and the size of a merge of two single-entry maps.
        await Assert.That(Run($"map:contains(map {{ {left} : 1 }}, {right})")).IsEqualTo(expected);
        await Assert.That(Run($"map:contains(map {{ {right} : 1 }}, {left})")).IsEqualTo(expected);
        var size = expected == "boolean:True" ? "integer:1" : "integer:2";
        await Assert.That(Run($"map:size(map:merge((map:entry({left}, 1), map:entry({right}, 2))))")).IsEqualTo(size);
        await Assert.That(Run($"exists(map {{ {left} : 1 }}({right}))")).IsEqualTo(expected);
    }

    [Test]
    public async Task Merging_TheSameKeyTwice_KeepsTheFirstKeyWithItsType(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Run("map:keys(map:merge((map:entry(1, 'a'), map:entry(1.0, 'b'))))")).IsEqualTo("integer:1");
    }
}
