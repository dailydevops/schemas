namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using static DerivedFixture;
using static NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core.CastFixture;

/// <summary>
/// Casting to the derived string types (Functions and Operators, 17.2 to 17.5): the value is cast to <c>xs:string</c>, the whitespace of the
/// target is applied, and the lexical space of the target is checked (FORG0001).
/// </summary>
public class DerivedStringCastTests
{
    // normalizedString replaces tab, line feed, and carriage return by a space; token also collapses runs and trims.
    [Test]
    [Arguments("xs:normalizedString('  a \t b \n c \r d  ')", "normalizedString:  a   b   c   d  ")]
    [Arguments("xs:normalizedString(' a  b ')", "normalizedString: a  b ")]
    [Arguments("xs:normalizedString('\t')", "normalizedString: ")]
    [Arguments("xs:normalizedString('')", "normalizedString:")]
    [Arguments("xs:token('  a \t b \n c \r d  ')", "token:a b c d")]
    [Arguments("xs:token(' a  b ')", "token:a b")]
    [Arguments("xs:token('\t')", "token:")]
    [Arguments("xs:token('')", "token:")]
    [Arguments("' x ' cast as xs:token", "token:x")]
    [Arguments("' x ' cast as xs:normalizedString", "normalizedString: x ")]
    [Arguments("xs:untypedAtomic(' x  y ') cast as xs:token", "token:x y")]
    public async Task Whitespace_IsAppliedAsTheTargetStates(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("language", "en", "en")]
    [Arguments("language", "en-US", "en-US")]
    [Arguments("language", " en-GB ", "en-GB")]
    [Arguments("language", "abcdefgh", "abcdefgh")]
    [Arguments("language", "x-1234abcd", "x-1234abcd")]
    [Arguments("Name", "a", "a")]
    [Arguments("Name", "a:b", "a:b")]
    [Arguments("Name", "_x-1.2", "_x-1.2")]
    [Arguments("Name", " abc ", "abc")]
    [Arguments("NCName", "abc", "abc")]
    [Arguments("NCName", " a-b.c_d ", "a-b.c_d")]
    [Arguments("ID", "abc", "abc")]
    [Arguments("IDREF", "abc", "abc")]
    [Arguments("ENTITY", "abc", "abc")]
    [Arguments("NMTOKEN", "1abc", "1abc")]
    [Arguments("NMTOKEN", "-x.y:z", "-x.y:z")]
    [Arguments("NMTOKEN", " a ", "a")]
    public async Task ValueInTheLexicalSpace_IsAccepted_WithTheTokenWhitespace(
        string type,
        string value,
        string expected
    ) => await Assert.That(Show($"'{value}' cast as xs:{type}")).IsEqualTo($"{type}:{expected}");

    [Test]
    [Arguments("language", "")]
    [Arguments("language", "toolongsubtag")]
    [Arguments("language", "en_US")]
    [Arguments("language", "1a")]
    [Arguments("language", "en-")]
    [Arguments("language", "-en")]
    [Arguments("language", "a b")]
    [Arguments("language", "en-toolongsubtag")]
    [Arguments("Name", "")]
    [Arguments("Name", "1a")]
    [Arguments("Name", "a b")]
    [Arguments("Name", "-a")]
    [Arguments("NCName", "a:b")]
    [Arguments("NCName", ":a")]
    [Arguments("NCName", "1a")]
    [Arguments("NCName", "a b")]
    [Arguments("NCName", "")]
    [Arguments("ID", "a:b")]
    [Arguments("ID", "1a")]
    [Arguments("IDREF", "a:b")]
    [Arguments("IDREF", "")]
    [Arguments("ENTITY", "a:b")]
    [Arguments("ENTITY", "a b")]
    [Arguments("NMTOKEN", "")]
    [Arguments("NMTOKEN", "a b")]
    [Arguments("NMTOKEN", "a,b")]
    public async Task ValueOutsideTheLexicalSpace_RaisesForg0001_AndIsNotCastable(string type, string value)
    {
        await Assert.That(Code($"'{value}' cast as xs:{type}")).IsEqualTo("FORG0001");
        await Assert.That(Code($"xs:{type}('{value}')")).IsEqualTo("FORG0001");
        await Assert.That(Castable($"'{value}'", type)).IsFalse();
    }

    [Test]
    public async Task EveryDerivedStringType_AcceptsAnOrdinaryWord_AndIsCastable()
    {
        foreach (var type in StringTypes)
        {
            await Assert.That(Show($"'word' cast as xs:{type}")).IsEqualTo($"{type}:word");
            await Assert.That(Castable("'word'", type)).IsTrue().Because(type);
            await Assert.That(Evaluate($"xs:{type}(())").Count).IsEqualTo(0).Because(type);
        }
    }

    // The value goes through xs:string first, so a source of any primitive type is allowed and its canonical string is checked.
    [Test]
    [Arguments("5 cast as xs:token", "token:5")]
    [Arguments("5 cast as xs:NMTOKEN", "NMTOKEN:5")]
    [Arguments("xs:decimal('1.50') cast as xs:normalizedString", "normalizedString:1.5")]
    [Arguments("xs:boolean('1') cast as xs:Name", "Name:true")]
    [Arguments("xs:double('1e3') cast as xs:NMTOKEN", "NMTOKEN:1000")]
    [Arguments("xs:date('2000-01-31') cast as xs:token", "token:2000-01-31")]
    [Arguments("xs:time('10:00:00') cast as xs:NMTOKEN", "NMTOKEN:10:00:00")]
    [Arguments("xs:duration('P1Y') cast as xs:ID", "ID:P1Y")]
    [Arguments("xs:hexBinary('0a') cast as xs:NMTOKEN", "NMTOKEN:0A")]
    [Arguments("xs:anyURI('abc') cast as xs:IDREF", "IDREF:abc")]
    [Arguments("xs:QName('abc') cast as xs:NCName", "NCName:abc")]
    public async Task FromAnotherPrimitive_IsCastThroughItsStringValue(string expression, string expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("xs:time('10:00:00') cast as xs:NCName", "FORG0001")]
    [Arguments("xs:dateTime('2000-01-31T10:00:00') cast as xs:ID", "FORG0001")]
    [Arguments("xs:double('1e3') cast as xs:language", "FORG0001")]
    [Arguments("5 cast as xs:Name", "FORG0001")]
    [Arguments("xs:hexBinary('0a') cast as xs:language", "FORG0001")]
    [Arguments("xs:anyURI('http://example.com/') cast as xs:NCName", "FORG0001")]
    [Arguments("(1, 2) cast as xs:token", "XPTY0004")]
    [Arguments("() cast as xs:token", "XPTY0004")]
    public async Task FromAnotherPrimitive_OutsideTheLexicalSpace_RaisesItsCode(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    // The derived string types carry the type annotation of the target.
    [Test]
    [Arguments("xs:token(' a ') instance of xs:token", true)]
    [Arguments("xs:token(' a ') instance of xs:normalizedString", true)]
    [Arguments("xs:token(' a ') instance of xs:string", true)]
    [Arguments("xs:token(' a ') instance of xs:NCName", false)]
    [Arguments("xs:NCName('a') instance of xs:Name", true)]
    [Arguments("xs:NCName('a') instance of xs:token", true)]
    [Arguments("xs:ID('a') instance of xs:NCName", true)]
    [Arguments("xs:ID('a') instance of xs:IDREF", false)]
    [Arguments("xs:language('en') instance of xs:token", true)]
    [Arguments("xs:NMTOKEN('a') instance of xs:Name", false)]
    [Arguments("'a' instance of xs:token", false)]
    public async Task Result_CarriesTheTypeAnnotationOfTheTarget(string expression, bool expected) =>
        await Assert.That(Show(expression)).IsEqualTo(expected ? "boolean:true" : "boolean:false");
}
