namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using static CastFixture;

/// <summary>
/// The lexical form that a cast to <c>xs:string</c> and <c>xs:untypedAtomic</c> gives (Functions and Operators, 17.1.2): a double or float
/// in the range of 1e-6 up to 1e6 uses plain notation, a value outside it the scientific notation with at least one fraction digit.
/// </summary>
public class CastStringFormTests
{
    [Test]
    [Arguments("xs:double('1')", "1")]
    [Arguments("xs:double('1.5')", "1.5")]
    [Arguments("xs:double('100')", "100")]
    [Arguments("xs:double('-100')", "-100")]
    [Arguments("xs:double('0.1')", "0.1")]
    [Arguments("xs:double('12345.678')", "12345.678")]
    [Arguments("xs:double('999999')", "999999")]
    [Arguments("xs:double('999999.5')", "999999.5")]
    [Arguments("xs:double('1000000')", "1.0E6")]
    [Arguments("xs:double('123456789')", "1.23456789E8")]
    [Arguments("xs:double('-123456789')", "-1.23456789E8")]
    [Arguments("xs:double('0.000001')", "0.000001")]
    [Arguments("xs:double('0.0000015')", "0.0000015")]
    [Arguments("xs:double('0.0000001')", "1.0E-7")]
    [Arguments("xs:double('1.234E-7')", "1.234E-7")]
    [Arguments("xs:double('1e300')", "1.0E300")]
    [Arguments("xs:double('0')", "0")]
    [Arguments("xs:double('-0')", "-0")]
    [Arguments("xs:double('NaN')", "NaN")]
    [Arguments("xs:double('INF')", "INF")]
    [Arguments("xs:double('-INF')", "-INF")]
    [Arguments("xs:float('1.1')", "1.1")]
    [Arguments("xs:float('0.3')", "0.3")]
    [Arguments("xs:float('16777216')", "1.6777216E7")]
    [Arguments("xs:float('3.4028235E38')", "3.4028235E38")]
    [Arguments("xs:float('1e-7')", "1.0E-7")]
    [Arguments("xs:float('0.000001')", "0.000001")]
    [Arguments("xs:float('-0')", "-0")]
    [Arguments("xs:float('INF')", "INF")]
    [Arguments("xs:float('NaN')", "NaN")]
    [Arguments("xs:decimal('1.50')", "1.5")]
    [Arguments("xs:decimal('100.0')", "100")]
    [Arguments("xs:decimal('-0.5')", "-0.5")]
    [Arguments("xs:decimal('0.000')", "0")]
    [Arguments("xs:decimal('0.0000001')", "0.0000001")]
    [Arguments("xs:decimal('123456789012345678901234567890.5')", "123456789012345678901234567890.5")]
    [Arguments("xs:integer('12')", "12")]
    [Arguments("xs:integer('-007')", "-7")]
    [Arguments("xs:integer('0')", "0")]
    [Arguments("xs:integer('9223372036854775807')", "9223372036854775807")]
    [Arguments("xs:integer('-9223372036854775808')", "-9223372036854775808")]
    [Arguments("xs:boolean('1')", "true")]
    [Arguments("xs:boolean('0')", "false")]
    public async Task Value_AsString_UsesTheCanonicalFormOfItsType(string value, string expected)
    {
        await Assert.That(EvaluateRendered(value + " cast as xs:string")).IsEqualTo("string:" + expected);
        await Assert.That(EvaluateRendered(value + " cast as xs:untypedAtomic")).IsEqualTo("untyped:" + expected);
    }

    [Test]
    [Arguments("xs:double('1.5')")]
    [Arguments("xs:double('1000000')")]
    [Arguments("xs:double('0.0000001')")]
    [Arguments("xs:double('123456789.125')")]
    [Arguments("xs:double('-0')")]
    [Arguments("xs:float('1.1')")]
    [Arguments("xs:float('3.4028235E38')")]
    [Arguments("xs:decimal('-0.0625')")]
    [Arguments("xs:integer('-42')")]
    public async Task FormOfAValue_ParsesBackToTheSameValue(string value)
    {
        var form = EvaluateRendered(value + " cast as xs:string")["string:".Length..];
        var target = EvaluateRendered(value).Split(':')[0];

        await Assert.That(EvaluateRendered("'" + form + "' cast as xs:" + target)).IsEqualTo(EvaluateRendered(value));
    }
}
