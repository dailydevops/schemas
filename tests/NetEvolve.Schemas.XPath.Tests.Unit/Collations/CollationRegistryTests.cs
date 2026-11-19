namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Collations;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// The collation registry (Functions and Operators 1.0, 7.3): the codepoint collation is the only built-in collation, every other
/// absolute URI is FOCH0002, a relative URI is resolved against the static base URI before the lookup, and a host adds its own collation
/// through <see cref="CollationRegistry.With"/>.
/// </summary>
public class CollationRegistryTests
{
    private const string Codepoint = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    [Test]
    public async Task Default_FindsTheCodepointCollationByItsAbsoluteUri()
    {
        var found = CollationRegistry.Default.TryResolve(Codepoint, null, out var collation);

        await Assert.That(found).IsTrue();
        await Assert.That(collation).IsSameReferenceAs(CodepointCollation.Instance);
        await Assert.That(CodepointCollation.Instance.Uri).IsEqualTo(Codepoint);
    }

    [Test]
    [Arguments("http://www.example.com/COLLATION/NOT/SUPPORTED")]
    [Arguments("http://www.w3.org/2013/collation/UCA?lang=en;strength=primary")]
    [Arguments("http://www.w3.org/2013/collation/UCA")]
    [Arguments("http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive")]
    [Arguments("http://www.w3.org/2005/xpath-functions/collation/codepoint/")]
    [Arguments("urn:collation:codepoint")]
    public async Task Default_UnknownAbsoluteUri_IsFOCH0002(string collation)
    {
        await Assert.That(CollationRegistry.Default.TryResolve(collation, null, out var found)).IsFalse();
        await Assert.That(found).IsNull();

        var failure = await Assert
            .That(() => CollationRegistry.Default.Resolve(collation, null))
            .Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("FOCH0002");
    }

    [Test]
    public async Task RelativeUri_IsResolvedAgainstTheStaticBaseUriBeforeTheLookup()
    {
        var registry = CollationRegistry.Default.With(new HostCollations.CaseBlind());

        var sibling = registry.TryResolve("case-blind", new Uri("http://example.com/collations/doc.xml"), out var a);
        var parent = registry.TryResolve(
            "../collations/case-blind",
            new Uri("http://example.com/x/doc.xml"),
            out var b
        );
        var rooted = registry.TryResolve(
            "/collations/case-blind",
            new Uri("http://example.com/x/y/doc.xml"),
            out var c
        );

        await Assert.That(sibling).IsTrue();
        await Assert.That(parent).IsTrue();
        await Assert.That(rooted).IsTrue();
        await Assert.That(a!.Uri).IsEqualTo(HostCollations.CaseBlindUri);
        await Assert.That(b).IsSameReferenceAs(a);
        await Assert.That(c).IsSameReferenceAs(a);
    }

    [Test]
    public async Task RelativeUri_ThatResolvesToTheCodepointUri_FindsTheCodepointCollation()
    {
        var found = CollationRegistry.Default.TryResolve(
            "collation/codepoint",
            new Uri("http://www.w3.org/2005/xpath-functions/doc.xml"),
            out var collation
        );

        await Assert.That(found).IsTrue();
        await Assert.That(collation).IsSameReferenceAs(CodepointCollation.Instance);
    }

    [Test]
    public async Task RelativeUri_ThatResolvesToNoCollation_IsFOCH0002()
    {
        var failure = await Assert
            .That(() => CollationRegistry.Default.Resolve("CollationA", new Uri("http://example.com/dir/doc.xml")))
            .Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("FOCH0002");
    }

    [Test]
    public async Task RelativeUri_WithoutAnAbsoluteStaticBaseUri_IsFOCH0002()
    {
        var registry = CollationRegistry.Default.With(new HostCollations.CaseBlind());

        await Assert.That(registry.TryResolve("case-blind", null, out _)).IsFalse();
        await Assert.That(registry.TryResolve("case-blind", new Uri("dir/doc.xml", UriKind.Relative), out _)).IsFalse();

        var failure = await Assert.That(() => registry.Resolve("case-blind", null)).Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("FOCH0002");
    }

    [Test]
    public async Task AbsoluteUri_IgnoresTheStaticBaseUri()
    {
        var found = CollationRegistry.Default.TryResolve(
            Codepoint,
            new Uri("http://example.com/dir/doc.xml"),
            out var collation
        );

        await Assert.That(found).IsTrue();
        await Assert.That(collation).IsSameReferenceAs(CodepointCollation.Instance);
    }

    [Test]
    public async Task With_HostCollation_IsFoundByItsUri()
    {
        var host = new HostCollations.CaseBlind();

        var registry = CollationRegistry.Default.With(host);

        await Assert.That(registry.TryResolve(HostCollations.CaseBlindUri, null, out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(host);
        await Assert.That(registry.Resolve(Codepoint, null)).IsSameReferenceAs(CodepointCollation.Instance);
    }

    [Test]
    public async Task With_DoesNotChangeTheRegistryItIsCalledOn()
    {
        var registry = CollationRegistry.Default.With(new HostCollations.CaseBlind());

        await Assert.That(CollationRegistry.Default.TryResolve(HostCollations.CaseBlindUri, null, out _)).IsFalse();
        await Assert.That(registry).IsNotSameReferenceAs(CollationRegistry.Default);
    }

    [Test]
    public async Task With_TheUriOfARegisteredCollation_ReplacesIt()
    {
        var first = new HostCollations.CaseBlind();
        var second = new HostCollations.CaseBlind();

        var registry = CollationRegistry.Default.With(first).With(second);

        await Assert.That(registry.Resolve(HostCollations.CaseBlindUri, null)).IsSameReferenceAs(second);
    }

    [Test]
    public async Task With_Null_IsRejected() =>
        _ = await Assert.That(() => CollationRegistry.Default.With(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task TryResolve_NullUri_IsRejected() =>
        _ = await Assert
            .That(() => CollationRegistry.Default.TryResolve(null!, null, out _))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Collation_WithoutAUri_IsRejected() =>
        _ = await Assert.That(() => new HostCollations.CaseBlind(string.Empty)).Throws<ArgumentException>();

    [Test]
    [Arguments("relative/collation")]
    [Arguments("/rooted")]
    public async Task Collation_WithARelativeUri_IsRejected(string name) =>
        _ = await Assert.That(() => new HostCollations.CaseBlind(name)).Throws<ArgumentOutOfRangeException>();
}
