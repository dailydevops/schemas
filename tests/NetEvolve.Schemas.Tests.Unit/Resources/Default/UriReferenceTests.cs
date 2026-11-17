namespace NetEvolve.Schemas.Tests.Unit.Resources.Default;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Resources.Default;

internal sealed class UriReferenceTests
{
    private const string Base = "http://a/b/c/d;p?q";

    // RFC 3986 section 5.4.1.
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
    public async Task Resolve_Rfc3986NormalExamples_MatchExpected(string reference, string expected)
    {
        var resolution = UriReference.Resolve(reference, Base);

        await Assert.That(resolution.Uri).IsEqualTo(expected);
        await Assert.That(resolution.Code.Number).IsEqualTo(0);
    }

    // RFC 3986 section 5.4.2, strict parsing (AP-22a).
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
    public async Task Resolve_Rfc3986AbnormalExamples_MatchExpected(string reference, string expected)
    {
        var resolution = UriReference.Resolve(reference, Base);

        await Assert.That(resolution.Uri).IsEqualTo(expected);
        await Assert.That(resolution.Code.Number).IsEqualTo(0);
    }

    [Test]
    [Arguments("a.xsd", null)]
    [Arguments("a.xsd", "")]
    [Arguments("", null)]
    [Arguments("a.xsd", "rel/dir/")]
    [Arguments("a.xsd", "/abs/dir/")]
    [Arguments("?q", "//host/x")]
    public async Task Resolve_RelativeWithoutAbsoluteBase_YieldsBaseUriMissing(string reference, string? origin)
    {
        var resolution = UriReference.Resolve(reference, origin);

        await Assert.That(resolution.Uri).IsNull();
        await Assert.That(resolution.Code).IsEqualTo(CoreDiagnosticCodes.ResourceBaseUriMissing);
    }

    [Test]
    [Arguments("http://x/a/../b", null, "http://x/b")]
    [Arguments("file:///a/./b", "no-base", "file:///a/b")]
    [Arguments("urn:isbn:1", "http://a/b", "urn:isbn:1")]
    public async Task Resolve_AbsoluteReference_IgnoresTheBase(string reference, string? origin, string expected)
    {
        var resolution = UriReference.Resolve(reference, origin);

        await Assert.That(resolution.Uri).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", "http://a/b#f", "http://a/b")]
    [Arguments("g", "http://a", "http://a/g")]
    [Arguments("?y", "http://a", "http://a?y")]
    [Arguments("g", "http://a/b c/", "http://a/b%20c/g")]
    [Arguments("../x", "file:///d/e/f.xsd", "file:///d/x")]
    public async Task Resolve_BaseShapes_FollowRfc3986(string reference, string origin, string expected)
    {
        var resolution = UriReference.Resolve(reference, origin);

        await Assert.That(resolution.Uri).IsEqualTo(expected);
    }

    // AP-23 C: XML 1.0 section 4.2.2 escaping before validation, ResolvedUri is always ASCII.
    [Test]
    [Arguments("a b.xsd", "http://h/d/a%20b.xsd")]
    [Arguments("a<b>\"c", "http://h/d/a%3Cb%3E%22c")]
    [Arguments("a{b}|c\\d^e`f", "http://h/d/a%7Bb%7D%7Cc%5Cd%5Ee%60f")]
    [Arguments("a\tb\u007F", "http://h/d/a%09b%7F")]
    [Arguments("ä.xsd", "http://h/d/%C3%A4.xsd")]
    [Arguments("€", "http://h/d/%E2%82%AC")]
    [Arguments("😀", "http://h/d/%F0%9F%98%80")]
    [Arguments("a%41b", "http://h/d/a%41b")]
    public async Task Resolve_DisallowedCharacters_AreEscaped(string reference, string expected)
    {
        var resolution = UriReference.Resolve(reference, "http://h/d/x");

        await Assert.That(resolution.Uri).IsEqualTo(expected);
    }

    [Test]
    [Arguments("%")]
    [Arguments("a%")]
    [Arguments("a%z1")]
    [Arguments("a%zz")]
    [Arguments("a%4")]
    [Arguments("\uD800")]
    [Arguments("\uDC00x")]
    [Arguments("a#b#c")]
    [Arguments("1:b")]
    [Arguments("http://h/a[b")]
    [Arguments("http://h/a]b")]
    [Arguments("a:b c%")]
    public async Task Resolve_InvalidReference_YieldsReferenceInvalid(string reference)
    {
        var resolution = UriReference.Resolve(reference, "http://h/d/x");

        await Assert.That(resolution.Uri).IsNull();
        await Assert.That(resolution.Code).IsEqualTo(CoreDiagnosticCodes.ResourceReferenceInvalid);
    }

    [Test]
    [Arguments("http://[::1]/a", "http://[::1]/a")]
    [Arguments("http://u:p@h:80/a?b=c;d#e", "http://u:p@h:80/a?b=c;d#e")]
    public async Task Resolve_AuthorityWithBracketsAndUserInfo_IsAccepted(string reference, string expected)
    {
        var resolution = UriReference.Resolve(reference, null);

        await Assert.That(resolution.Uri).IsEqualTo(expected);
    }

    [Test]
    public async Task Resolve_LongReference_ResolvesWithoutTruncation()
    {
        var segment = new string('s', 700);

        var resolution = UriReference.Resolve("../" + segment + "/ä", "http://a/b/c/d");

        await Assert.That(resolution.Uri).IsEqualTo("http://a/b/" + segment + "/%C3%A4");
    }

    [Test]
    public async Task Resolve_NullReference_Throws() =>
        await Assert.That(() => UriReference.Resolve(null!, "http://a/")).Throws<ArgumentNullException>();
}
