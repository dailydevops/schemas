namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.QNames;

/// <summary>
/// <c>fn:resolve-uri</c> (Functions and Operators 1.0, 8.1): the reference resolution examples of RFC 3986 section 5.4, the errors
/// FORG0002 and FONS0005, and the one-argument form that reads the static base URI of the function.
/// </summary>
public class ResolveUriTests
{
    private const string Base = "string:http://a/b/c/d;p?q";

    [Test]
    [Arguments("g:h", "g:h")]
    [Arguments("g", "http://a/b/c/g")]
    [Arguments("./g", "http://a/b/c/g")]
    [Arguments("g/", "http://a/b/c/g/")]
    [Arguments("/g", "http://a/g")]
    [Arguments("//g", "http://g")]
    [Arguments("?y", "http://a/b/c/d;p?y")]
    [Arguments("g?y", "http://a/b/c/g?y")]
    [Arguments("#s", "http://a/b/c/d;p?q#s")]
    [Arguments("g#s", "http://a/b/c/g#s")]
    [Arguments("g?y#s", "http://a/b/c/g?y#s")]
    [Arguments(";x", "http://a/b/c/;x")]
    [Arguments("g;x", "http://a/b/c/g;x")]
    [Arguments("g;x?y#s", "http://a/b/c/g;x?y#s")]
    [Arguments("", "http://a/b/c/d;p?q")]
    [Arguments(".", "http://a/b/c/")]
    [Arguments("./", "http://a/b/c/")]
    [Arguments("..", "http://a/b/")]
    [Arguments("../", "http://a/b/")]
    [Arguments("../g", "http://a/b/g")]
    [Arguments("../..", "http://a/")]
    [Arguments("../../", "http://a/")]
    [Arguments("../../g", "http://a/g")]
    public async Task ResolveUri_TheNormalExamplesOfRfc3986Section541(string relative, string expected) =>
        await Assert.That(QNamesHarness.Run("resolve-uri", "string:" + relative, Base)).IsEqualTo("anyURI:" + expected);

    [Test]
    [Arguments("../../../g", "http://a/g")]
    [Arguments("../../../../g", "http://a/g")]
    [Arguments("/./g", "http://a/g")]
    [Arguments("/../g", "http://a/g")]
    [Arguments("g.", "http://a/b/c/g.")]
    [Arguments(".g", "http://a/b/c/.g")]
    [Arguments("g..", "http://a/b/c/g..")]
    [Arguments("..g", "http://a/b/c/..g")]
    [Arguments("./../g", "http://a/b/g")]
    [Arguments("./g/.", "http://a/b/c/g/")]
    [Arguments("g/./h", "http://a/b/c/g/h")]
    [Arguments("g/../h", "http://a/b/c/h")]
    [Arguments("g;x=1/./y", "http://a/b/c/g;x=1/y")]
    [Arguments("g;x=1/../y", "http://a/b/c/y")]
    [Arguments("g?y/./x", "http://a/b/c/g?y/./x")]
    [Arguments("g?y/../x", "http://a/b/c/g?y/../x")]
    [Arguments("g#s/./x", "http://a/b/c/g#s/./x")]
    [Arguments("g#s/../x", "http://a/b/c/g#s/../x")]
    [Arguments("http:g", "http:g")]
    public async Task ResolveUri_TheAbnormalExamplesOfRfc3986Section542(string relative, string expected) =>
        await Assert.That(QNamesHarness.Run("resolve-uri", "string:" + relative, Base)).IsEqualTo("anyURI:" + expected);

    [Test]
    [Arguments("anyURI:g", "anyURI:http://a/b/c/d;p?q", "anyURI:http://a/b/c/g")]
    [Arguments("untypedAtomic:g", "untypedAtomic:http://a/b/c/d;p?q", "anyURI:http://a/b/c/g")]
    [Arguments("()", "string:http://a/b/c/d;p?q", "")]
    [Arguments("()", "string:BaseValue", "")]
    public async Task ResolveUri_TheArgumentsMayBeUriOrUntypedValues_AndTheEmptySequenceGivesTheEmptySequence(
        string relative,
        string baseText,
        string expected
    ) => await Assert.That(QNamesHarness.Run("resolve-uri", relative, baseText)).IsEqualTo(expected);

    [Test]
    public async Task ResolveUri_AnAbsoluteReference_IsReturnedWithoutLookingAtTheBase()
    {
        await Assert
            .That(QNamesHarness.Run("resolve-uri", "string:http://www.examples.com", "string:"))
            .IsEqualTo("anyURI:http://www.examples.com");
        await Assert
            .That(QNamesHarness.Run("resolve-uri", "string:http://www.example.com/a.html", "string:b.html"))
            .IsEqualTo("anyURI:http://www.example.com/a.html");
        await Assert
            .That(QNamesHarness.Run("resolve-uri", "string:urn:isbn:01234567890X", "string:http://www.example.com/"))
            .IsEqualTo("anyURI:urn:isbn:01234567890X");
    }

    [Test]
    [Arguments("a.html", "b.html")]
    [Arguments("a.html", "")]
    [Arguments("b.html", "http://www.example.com/a.html#fragment")]
    [Arguments("examples", "http:%%")]
    [Arguments(":", "http://www.example.com/")]
    [Arguments("a b", "http://www.example.com/")]
    [Arguments("a<b>", "http://www.example.com/")]
    [Arguments("a%zz", "http://www.example.com/")]
    [Arguments("a", "http://www.example.com/that doc.html")]
    [Arguments("a", "http://www.example.com/{x}")]
    public async Task ResolveUri_AReferenceOrABaseThatIsNotAValidUri_IsFORG0002(string relative, string baseText) =>
        await Assert
            .That(QNamesHarness.Run("resolve-uri", "string:" + relative, "string:" + baseText))
            .IsEqualTo("error:FORG0002");

    [Test]
    public async Task ResolveUri_ACharacterBeyondAscii_IsEscapedLikeIriToUri() =>
        await Assert
            .That(QNamesHarness.Run("resolve-uri", "string:\u00E9", "string:http://www.example.com/"))
            .IsEqualTo("anyURI:http://www.example.com/%C3%A9");

    [Test]
    [Arguments("string:a", "()")]
    [Arguments("integer:1", "string:http://a/")]
    [Arguments("string:a", "integer:1")]
    [Arguments("string:a|string:b", "string:http://a/")]
    public async Task ResolveUri_AnArgumentOfTheWrongTypeOrCardinality_IsXPTY0004(string relative, string baseText) =>
        await Assert.That(QNamesHarness.Run("resolve-uri", relative, baseText)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task ResolveUri_WithoutAStaticBaseUri_AnAbsoluteReferenceIsReturnedAndARelativeOneIsFONS0005()
    {
        await Assert.That(QNamesHarness.Run("resolve-uri", "string:http://a/b")).IsEqualTo("anyURI:http://a/b");
        await Assert.That(QNamesHarness.Run("resolve-uri", "string:b")).IsEqualTo("error:FONS0005");
        await Assert.That(QNamesHarness.Run("resolve-uri", "string:")).IsEqualTo("error:FONS0005");
        await Assert.That(QNamesHarness.Run("resolve-uri", "()")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveUri_WithAStaticBaseUri_TheOneArgumentFormResolvesAgainstIt()
    {
        var function = new ResolveUriFunction(new Uri("http://a/b/c/d;p?q"));

        await Assert.That(QNamesHarness.RunFunction(function, null, "string:g")).IsEqualTo("anyURI:http://a/b/c/g");
        await Assert.That(QNamesHarness.RunFunction(function, null, "string:")).IsEqualTo("anyURI:http://a/b/c/d;p?q");
        await Assert
            .That(QNamesHarness.RunFunction(function, null, "string:g", "string:http://x/y/z"))
            .IsEqualTo("anyURI:http://x/y/g");
    }

    [Test]
    public async Task ResolveUri_ARelativeStaticBaseUri_IsNoBaseUri()
    {
        var function = new ResolveUriFunction(new Uri("sub/dir", UriKind.Relative));

        await Assert.That(QNamesHarness.RunFunction(function, null, "string:g")).IsEqualTo("error:FONS0005");
    }
}
