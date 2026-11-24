namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Other;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Temporal.CastShow;

/// <summary>The values and errors of the casts among <c>xs:hexBinary</c>, <c>xs:base64Binary</c>, <c>xs:anyURI</c>, and the string types.</summary>
public class BinaryAndUriCastTests
{
    // Both binary types share the octets: a cast between them keeps them and changes the lexical form.
    [Test]
    [Arguments("xs:hexBinary('0FB7') cast as xs:base64Binary", "Base64Binary:D7c=")]
    [Arguments("xs:base64Binary('D7c=') cast as xs:hexBinary", "HexBinary:0FB7")]
    [Arguments("xs:hexBinary('') cast as xs:base64Binary", "Base64Binary:")]
    [Arguments("xs:base64Binary('') cast as xs:hexBinary", "HexBinary:")]
    [Arguments("xs:hexBinary('0fb7') cast as xs:hexBinary", "HexBinary:0FB7")]
    [Arguments("xs:base64Binary('D7c=') cast as xs:base64Binary", "Base64Binary:D7c=")]
    [Arguments("xs:hexBinary('F43D1234ce8f') cast as xs:base64Binary", "Base64Binary:9D0SNM6P")]
    public async Task BetweenTheBinaryTypes_KeepsTheOctets(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // xs:string source: the lexical form goes through the datatype of the target and whitespace collapses.
    [Test]
    [Arguments("' 0FB7 ' cast as xs:hexBinary", "HexBinary:0FB7")]
    [Arguments("'0fb7' cast as xs:hexBinary", "HexBinary:0FB7")]
    [Arguments("'' cast as xs:hexBinary", "HexBinary:")]
    [Arguments("' D7c= ' cast as xs:base64Binary", "Base64Binary:D7c=")]
    [Arguments("'aaa a' cast as xs:base64Binary", "Base64Binary:aaaa")]
    [Arguments("'' cast as xs:base64Binary", "Base64Binary:")]
    [Arguments("xs:untypedAtomic('0FB7') cast as xs:hexBinary", "HexBinary:0FB7")]
    [Arguments("xs:untypedAtomic('D7c=') cast as xs:base64Binary", "Base64Binary:D7c=")]
    public async Task FromString_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // To the string types the canonical form: upper-case digits for hexBinary, no spaces for base64Binary.
    [Test]
    [Arguments("xs:hexBinary('0fB7') cast as xs:string", "XsString:0FB7")]
    [Arguments("xs:hexBinary('') cast as xs:string", "XsString:")]
    [Arguments("xs:base64Binary('aaa a') cast as xs:string", "XsString:aaaa")]
    [Arguments("xs:base64Binary('0w==') cast as xs:string", "XsString:0w==")]
    [Arguments("xs:hexBinary('0FB7') cast as xs:untypedAtomic", "UntypedAtomic:0FB7")]
    [Arguments("xs:base64Binary('D7c=') cast as xs:untypedAtomic", "UntypedAtomic:D7c=")]
    [Arguments("xs:hexBinary(xs:base64Binary('Ow==')) cast as xs:string", "XsString:3B")]
    public async Task ToStringTypes_GivesTheCanonicalForm(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    // xs:anyURI: any string without a malformed percent escape or an empty scheme is a URI, whitespace collapses, and the value is the string.
    [Test]
    [Arguments("'example.com/' cast as xs:anyURI", "AnyUri:example.com/")]
    [Arguments("'' cast as xs:anyURI", "AnyUri:")]
    [Arguments("'  http://example.com/a  b ' cast as xs:anyURI", "AnyUri:http://example.com/a b")]
    [Arguments("xs:untypedAtomic('example.com/') cast as xs:anyURI", "AnyUri:example.com/")]
    [Arguments("xs:anyURI('example.com/') cast as xs:anyURI", "AnyUri:example.com/")]
    [Arguments("xs:anyURI('http://example.com/a') cast as xs:string", "XsString:http://example.com/a")]
    [Arguments("xs:anyURI('http://example.com/a') cast as xs:untypedAtomic", "UntypedAtomic:http://example.com/a")]
    [Arguments("xs:anyURI('mailto:a%20b') cast as xs:string", "XsString:mailto:a%20b")]
    [Arguments("'a%4Fb' cast as xs:anyURI", "AnyUri:a%4Fb")]
    [Arguments("'/a:b' cast as xs:anyURI", "AnyUri:/a:b")]
    public async Task AnyUri_ProducesTheValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    private static (string, string)[] Failures() =>
        [
            ("'0FB' cast as xs:hexBinary", "FORG0001"),
            ("'0G' cast as xs:hexBinary", "FORG0001"),
            ("'0F B7' cast as xs:hexBinary", "FORG0001"),
            ("xs:untypedAtomic('xyz') cast as xs:hexBinary", "FORG0001"),
            ("'a' cast as xs:base64Binary", "FORG0001"),
            ("'a!aa' cast as xs:base64Binary", "FORG0001"),
            ("'aaa' cast as xs:base64Binary", "FORG0001"),
            ("xs:untypedAtomic('a') cast as xs:base64Binary", "FORG0001"),
            ("xs:integer('1') cast as xs:hexBinary", "XPTY0004"),
            ("xs:double('1') cast as xs:base64Binary", "XPTY0004"),
            ("xs:boolean('true') cast as xs:anyURI", "XPTY0004"),
            ("xs:dateTime('2004-10-13T12:30:00') cast as xs:hexBinary", "XPTY0004"),
            ("xs:duration('P1Y') cast as xs:anyURI", "XPTY0004"),
            ("xs:hexBinary('0F') cast as xs:integer", "XPTY0004"),
            ("xs:base64Binary('Dw==') cast as xs:boolean", "XPTY0004"),
            ("xs:anyURI('http://a') cast as xs:double", "XPTY0004"),
            ("xs:anyURI('http://a') cast as xs:hexBinary", "XPTY0004"),
            ("xs:hexBinary('0F') cast as xs:anyURI", "XPTY0004"),
            ("xs:anyURI('http://a') cast as xs:date", "XPTY0004"),
            ("'%' cast as xs:anyURI", "FORG0001"),
            ("'%gg' cast as xs:anyURI", "FORG0001"),
            ("'http://www.example.com/file%GF.html' cast as xs:anyURI", "FORG0001"),
            ("'%4' cast as xs:anyURI", "FORG0001"),
            ("':/cut.jpg' cast as xs:anyURI", "FORG0001"),
            ("':/' cast as xs:anyURI", "FORG0001"),
            ("xs:untypedAtomic('%') cast as xs:anyURI", "FORG0001"),
            ("() cast as xs:hexBinary", "XPTY0004"),
            ("('0F', '0F') cast as xs:hexBinary", "XPTY0004"),
        ];

    public static IEnumerable<Func<(string Expression, string Code)>> FailureCases()
    {
        foreach (var entry in Failures())
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(FailureCases))]
    public async Task Cast_Failure_RaisesItsCode(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);

    [Test]
    [MethodDataSource(nameof(FailureCases))]
    public async Task Castable_EveryFailingCast_ReturnsFalseAndRaisesNothing(string expression, string code)
    {
        _ = code;
        var castable = CastFixture.AsCastable(expression);

        await Assert.That(CastFixture.Code(castable)).IsEqualTo(string.Empty);
        _ = CastFixture.Evaluate(castable)[0].TryGetBoolean(out var value);
        await Assert.That(value).IsFalse();
    }

    [Test]
    [Arguments("xs:hexBinary(())")]
    [Arguments("xs:base64Binary(())")]
    [Arguments("xs:anyURI(())")]
    public async Task Constructor_OfTheEmptySequence_IsTheEmptySequence(string expression) =>
        await Assert.That(CastFixture.Evaluate(expression).Count).IsEqualTo(0);

    [Test]
    [Arguments("xs:hexBinary('0FB7')", "HexBinary")]
    [Arguments("xs:base64Binary('D7c=')", "Base64Binary")]
    [Arguments("xs:anyURI('http://example.com/')", "AnyUri")]
    [Arguments("xs:hexBinary(xs:base64Binary('D7c='))", "HexBinary")]
    [Arguments("xs:base64Binary(xs:hexBinary('0FB7'))", "Base64Binary")]
    public async Task Constructor_ProducesAnItemOfItsType(string expression, string kind) =>
        await Assert.That(KindOf(expression).ToString()).IsEqualTo(kind);

    [Test]
    [Arguments("xs:hexBinary('0G')", "FORG0001")]
    [Arguments("xs:base64Binary('a')", "FORG0001")]
    [Arguments("xs:hexBinary(1)", "XPTY0004")]
    [Arguments("xs:anyURI(1)", "XPTY0004")]
    [Arguments("xs:hexBinary(('0F', '0F'))", "XPTY0004")]
    public async Task Constructor_Error_RaisesItsCode(string expression, string code) =>
        await Assert.That(CastFixture.Code(expression)).IsEqualTo(code);
}
