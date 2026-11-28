namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Functions.QNames;

/// <summary>
/// <c>fn:base-uri</c> and <c>fn:static-base-uri</c> (Functions and Operators 1.0, 14.5 and 8.2): the base URI of a node as
/// <c>xs:anyURI</c> with the explicit node, the context item, the empty sequence, and a missing or non-node context item; the
/// static base URI that a function instance holds.
/// </summary>
public class BaseUriTests
{
    [Test]
    [Arguments("r", "anyURI:http://example.com/dir/file.xml")]
    [Arguments("r@id", "anyURI:http://example.com/dir/file.xml")]
    [Arguments("a", "anyURI:http://example.com/dir/file.xml")]
    [Arguments("b/t", "anyURI:http://example.com/dir/file.xml")]
    [Arguments("d", "anyURI:http://example.com/dir/sub/")]
    [Arguments("o", "")]
    [Arguments("q", "")]
    [Arguments("o.doc", "")]
    public async Task BaseUri_GivesTheBaseUriOfTheNodeOrTheEmptySequence(string node, string expected)
    {
        await Assert.That(QNamesHarness.Run("base-uri", "node:" + node)).IsEqualTo(expected);
        await Assert.That(QNamesHarness.RunWithContextItem("node:" + node, "base-uri")).IsEqualTo(expected);
    }

    [Test]
    public async Task BaseUri_TheEmptySequence_GivesTheEmptySequence() =>
        await Assert.That(QNamesHarness.Run("base-uri", "()")).IsEqualTo(string.Empty);

    [Test]
    public async Task BaseUri_WithoutArgumentAndWithoutContextItem_IsXPDY0002() =>
        await Assert.That(QNamesHarness.Run("base-uri")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task BaseUri_ANonNodeContextItem_IsXPTY0004() =>
        await Assert.That(QNamesHarness.RunWithContextItem("integer:1", "base-uri")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("string:r")]
    [Arguments("integer:1")]
    [Arguments("node:r|node:a")]
    public async Task BaseUri_AnArgumentThatIsNotAtMostOneNode_IsXPTY0004(string argument) =>
        await Assert.That(QNamesHarness.Run("base-uri", argument)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task StaticBaseUri_WithoutAStaticBaseUri_GivesTheEmptySequence() =>
        await Assert.That(QNamesHarness.Run("static-base-uri")).IsEqualTo(string.Empty);

    [Test]
    public async Task StaticBaseUri_AFunctionHoldingAnAbsoluteUri_GivesItAsAnyUri()
    {
        var function = new StaticBaseUriFunction(new Uri("http://example.com/base/"));

        await Assert.That(QNamesHarness.RunFunction(function, null)).IsEqualTo("anyURI:http://example.com/base/");
    }

    [Test]
    public async Task StaticBaseUri_ARelativeUri_IsNoStaticBaseUri()
    {
        var function = new StaticBaseUriFunction(new Uri("sub/dir", UriKind.Relative));

        await Assert.That(QNamesHarness.RunFunction(function, null)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task StaticBaseUri_UnderAStaticContextWithABaseUri_GivesTheStaticBaseUri() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(new Uri("http://www.example.com"), "static-base-uri"))
            .IsEqualTo("anyURI:http://www.example.com");

    [Test]
    public async Task StaticBaseUri_UnderAStaticContextWithARelativeBaseUri_GivesTheEmptySequence() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(new Uri("sub/dir", UriKind.Relative), "static-base-uri"))
            .IsEqualTo(string.Empty);

    [Test]
    public async Task StaticBaseUri_UnderAStaticContextWithoutABaseUri_GivesTheEmptySequence() =>
        await Assert.That(QNamesHarness.RunWithStaticBaseUri(null, "static-base-uri")).IsEqualTo(string.Empty);

    [Test]
    public async Task ResolveUri_OneArgumentUnderAStaticContextWithABaseUri_ResolvesAgainstIt() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(new Uri("http://example.com/dir/"), "resolve-uri", "string:a/b"))
            .IsEqualTo("anyURI:http://example.com/dir/a/b");

    [Test]
    public async Task ResolveUri_OneArgumentUnderAStaticContextWithoutABaseUri_IsFONS0005() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(null, "resolve-uri", "string:a/b"))
            .IsEqualTo("error:FONS0005");

    [Test]
    public async Task ResolveUri_TwoArgumentsUnderAStaticContextWithABaseUri_KeepTheExplicitBase() =>
        await Assert
            .That(
                QNamesHarness.RunWithStaticBaseUri(
                    new Uri("http://example.com/dir/"),
                    "resolve-uri",
                    "string:a",
                    "string:http://other.example/x/"
                )
            )
            .IsEqualTo("anyURI:http://other.example/x/a");

    [Test]
    public async Task OtherFunction_UnderAStaticContextWithABaseUri_IsLeftUnbound() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(new Uri("http://example.com/dir/"), "string", "integer:7"))
            .IsEqualTo("string:7");

    [Test]
    public async Task UnregisteredFunction_UnderAStaticContextWithABaseUri_IsNotResolved() =>
        await Assert
            .That(QNamesHarness.RunWithStaticBaseUri(new Uri("http://example.com/dir/"), "no-such-function"))
            .IsEqualTo("error:not-registered");
}
