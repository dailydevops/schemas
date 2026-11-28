namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>The XSD 1.0 lexical space of anyURI: RFC 2396 URI references with the escaping of XLink for non-ASCII characters.</summary>
public class AnyUriReferenceTests
{
    [Test]
    [Arguments("")]
    [Arguments("a")]
    [Arguments("/")]
    [Arguments("//")]
    [Arguments("anyURI:c")]
    [Arguments("http://example.org/a%20b?q=1&r=2#frag")]
    [Arguments("xあy")]
    [Arguments("a b")]
    [Arguments("@$()_-+..~!*")]
    [Arguments("foo<bar")]
    [Arguments("foo>bar")]
    [Arguments("foo\"bar")]
    [Arguments("http://[::1]/")]
    [Arguments(" b:c ")]
    public async Task IsValid_UriReference_ReturnsTrue(string value) =>
        await Assert.That(AnyUriReference.IsValid(value)).IsTrue();

    [Test]
    [Arguments(":a")]
    [Arguments("b:")]
    [Arguments("1a:b")]
    [Arguments("a_b:c")]
    [Arguments("b:#f")]
    [Arguments("a\b")]
    [Arguments("\\")]
    [Arguments("^")]
    [Arguments("%")]
    [Arguments("%4")]
    [Arguments("%zz")]
    [Arguments("a#b#c")]
    [Arguments("a{b")]
    public async Task IsValid_NoUriReference_ReturnsFalse(string value) =>
        await Assert.That(AnyUriReference.IsValid(value)).IsFalse();
}
