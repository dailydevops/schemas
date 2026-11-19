namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using static DerivedFixture;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core.CastFixture;

/// <summary>
/// Casting from a derived type (Functions and Operators, 17.3), within a branch of the hierarchy (17.4), and across the hierarchy (17.5): the
/// value of the source is cast as its primitive ancestor, so the result never keeps the annotation of the source, and the facets of the target decide.
/// </summary>
public class DerivedHierarchyTests
{
    // Across the hierarchy: xs:byte and xs:unsignedByte overlap only in 0 to 127.
    [Test]
    [Arguments("xs:byte(0) cast as xs:unsignedByte", "unsignedByte:0")]
    [Arguments("xs:byte(5) cast as xs:unsignedByte", "unsignedByte:5")]
    [Arguments("xs:byte(127) cast as xs:unsignedByte", "unsignedByte:127")]
    [Arguments("xs:unsignedByte(0) cast as xs:byte", "byte:0")]
    [Arguments("xs:unsignedByte(127) cast as xs:byte", "byte:127")]
    [Arguments("xs:int(-1) cast as xs:long", "long:-1")]
    [Arguments("xs:short(-1) cast as xs:nonPositiveInteger", "nonPositiveInteger:-1")]
    [Arguments("xs:positiveInteger(1) cast as xs:negativeInteger", "")]
    public async Task AcrossTheHierarchy_InRange_ProducesTheValueOfTheTarget(string expression, string expected)
    {
        if (string.IsNullOrEmpty(expected))
        {
            await Assert.That(Code(expression)).IsEqualTo("FORG0001");
        }
        else
        {
            await Assert.That(Show(expression)).IsEqualTo(expected);
        }
    }

    [Test]
    [Arguments("xs:byte(-1) cast as xs:unsignedByte")]
    [Arguments("xs:byte(-128) cast as xs:unsignedByte")]
    [Arguments("xs:unsignedByte(128) cast as xs:byte")]
    [Arguments("xs:unsignedByte(255) cast as xs:byte")]
    [Arguments("xs:unsignedInt(2147483648) cast as xs:int")]
    [Arguments("xs:long(4294967296) cast as xs:unsignedInt")]
    [Arguments("xs:int(0) cast as xs:positiveInteger")]
    [Arguments("xs:nonNegativeInteger(0) cast as xs:negativeInteger")]
    [Arguments("xs:positiveInteger(1) cast as xs:nonPositiveInteger")]
    public async Task AcrossTheHierarchy_OutOfRange_RaisesForg0001(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0001");

    [Test]
    [Arguments("xs:byte(-1) castable as xs:unsignedByte", false)]
    [Arguments("xs:byte(1) castable as xs:unsignedByte", true)]
    [Arguments("xs:unsignedByte(200) castable as xs:byte", false)]
    [Arguments("xs:unsignedByte(100) castable as xs:byte", true)]
    [Arguments("xs:long(4294967296) castable as xs:unsignedInt", false)]
    [Arguments("xs:positiveInteger(1) castable as xs:nonPositiveInteger", false)]
    [Arguments("xs:byte(1) castable as xs:token", true)]
    [Arguments("xs:token('a b') castable as xs:NCName", false)]
    [Arguments("xs:token(' 5 ') castable as xs:byte", true)]
    [Arguments("xs:token('500') castable as xs:byte", false)]
    [Arguments("xs:date('2000-01-01') castable as xs:byte", false)]
    public async Task Castable_AcrossTheHierarchy_IsFalseWhereTheCastFails(string expression, bool expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected ? "boolean:true" : "boolean:false");

    // Within a branch: a value of a type is a value of every type it derives from, and of a derived type if it is in its bounds.
    [Test]
    [Arguments("xs:byte(5) cast as xs:short", "short:5")]
    [Arguments("xs:byte(5) cast as xs:byte", "byte:5")]
    [Arguments("xs:long(5) cast as xs:int", "int:5")]
    [Arguments("xs:long(40000) cast as xs:int", "int:40000")]
    [Arguments("xs:positiveInteger(5) cast as xs:nonNegativeInteger", "nonNegativeInteger:5")]
    [Arguments("xs:integer(5) cast as xs:unsignedByte", "unsignedByte:5")]
    [Arguments("xs:token('a') cast as xs:normalizedString", "normalizedString:a")]
    [Arguments("xs:NCName('a') cast as xs:Name", "Name:a")]
    [Arguments("xs:Name('a') cast as xs:NCName", "NCName:a")]
    [Arguments("xs:ID('a') cast as xs:IDREF", "IDREF:a")]
    [Arguments("xs:ID('a') cast as xs:NMTOKEN", "NMTOKEN:a")]
    [Arguments("xs:language('en-us') cast as xs:token", "token:en-us")]
    public async Task WithinABranch_ProducesTheValueOfTheTarget(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:long(40000) cast as xs:short")]
    [Arguments("xs:Name('a:b') cast as xs:NCName")]
    [Arguments("xs:token('a:b') cast as xs:NCName")]
    [Arguments("xs:NMTOKEN('1a') cast as xs:Name")]
    public async Task WithinABranch_OutsideTheTarget_RaisesForg0001(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0001");

    // From a derived type to a primitive type: the value is cast as its primitive ancestor, and the result has the annotation of the target.
    [Test]
    [Arguments("xs:byte(5) cast as xs:integer", "integer:5")]
    [Arguments("xs:unsignedLong(5) cast as xs:integer", "integer:5")]
    [Arguments("xs:positiveInteger(5) cast as xs:decimal", "decimal:5")]
    [Arguments("xs:int(5) cast as xs:float", "float:5")]
    [Arguments("xs:long(5) cast as xs:double", "double:5")]
    [Arguments("xs:byte(5) cast as xs:string", "string:5")]
    [Arguments("xs:byte(5) cast as xs:untypedAtomic", "untypedAtomic:5")]
    [Arguments("xs:byte(1) cast as xs:boolean", "boolean:true")]
    [Arguments("xs:byte(0) cast as xs:boolean", "boolean:false")]
    [Arguments("xs:token('a') cast as xs:string", "string:a")]
    [Arguments("xs:NCName('a') cast as xs:string", "string:a")]
    [Arguments("xs:token('a') cast as xs:untypedAtomic", "untypedAtomic:a")]
    [Arguments("xs:token(' 5 ') cast as xs:integer", "integer:5")]
    [Arguments("xs:token('1.5') cast as xs:decimal", "decimal:1.5")]
    [Arguments("xs:token('true') cast as xs:boolean", "boolean:true")]
    [Arguments("xs:token('2000-01-01') cast as xs:date", "date:2000-01-01")]
    [Arguments("xs:token('abc') cast as xs:anyURI", "anyURI:abc")]
    [Arguments("xs:token('0A') cast as xs:hexBinary", "hexBinary:0A")]
    public async Task FromADerivedType_ToAPrimitiveType_DropsTheDerivedAnnotation(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:token('x') cast as xs:integer", "FORG0001")]
    [Arguments("xs:byte(5) cast as xs:date", "XPTY0004")]
    [Arguments("xs:byte(5) cast as xs:hexBinary", "XPTY0004")]
    [Arguments("xs:positiveInteger(5) cast as xs:anyURI", "XPTY0004")]
    [Arguments("xs:token('a') cast as xs:QName", "XPTY0004")]
    [Arguments("xs:unsignedLong('18446744073709551615') cast as xs:integer", "FOCA0003")]
    // The last row is the limit of the integer item (#265, D3): a value of a derived type beyond 64 bits is a value of the derived type, and the cast
    // to xs:integer, which has the 64 bits, raises FOCA0003. XPath 2.0 casts a string literal only to xs:QName, so the token is XPTY0004.
    public async Task FromADerivedType_FollowsTheTableOfItsPrimitiveAncestor(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    // The derived annotation survives only where the type is kept: the arithmetic of the integer family gives xs:integer.
    [Test]
    [Arguments("xs:byte(5) + 1", "integer:6")]
    [Arguments("xs:unsignedByte(5) * xs:byte(2)", "integer:10")]
    [Arguments("xs:positiveInteger(5) + xs:decimal('0.5')", "decimal:5.5")]
    [Arguments("xs:unsignedLong('18446744073709551615') - 1", "integer:18446744073709551614")]
    [Arguments("xs:unsignedLong('18446744073709551615') + 1", "integer:18446744073709551616")]
    [Arguments("xs:nonNegativeInteger('100000000000000000000') + 1", "integer:100000000000000000001")]
    public async Task Arithmetic_OnDerivedIntegers_GivesTheirBaseType(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:byte(1) instance of xs:integer", true)]
    [Arguments("xs:byte(1) instance of xs:long", true)]
    [Arguments("xs:byte(1) instance of xs:byte", true)]
    [Arguments("xs:byte(1) instance of xs:decimal", true)]
    [Arguments("xs:byte(1) instance of xs:unsignedByte", false)]
    [Arguments("xs:integer(1) instance of xs:byte", false)]
    [Arguments("xs:unsignedByte(1) instance of xs:nonNegativeInteger", true)]
    [Arguments("xs:unsignedByte(1) instance of xs:positiveInteger", false)]
    [Arguments("xs:byte(5) cast as xs:integer instance of xs:byte", false)]
    [Arguments("xs:unsignedLong('18446744073709551615') instance of xs:unsignedLong", true)]
    [Arguments("xs:unsignedLong('18446744073709551615') instance of xs:integer", true)]
    public async Task Result_CarriesTheTypeAnnotationOfTheTarget(string expression, bool expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected ? "boolean:true" : "boolean:false");
}
