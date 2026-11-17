namespace NetEvolve.Schemas.Xsd.Tests.Unit.Api;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class WildcardSeamTests
{
    [Test]
    public async Task Seam_Wildcard_AllowsOnlyItsNamespaces_AndLogsTheName()
    {
        var log = new List<string>();
        var wildcard = new FakeWildcard(log, XsdProcessContents.Lax, "urn:a", "urn:b");

        var allowed = wildcard.Allows(new XPathQName("urn:a", "x"));
        var denied = wildcard.Allows(new XPathQName("urn:c", "x"));

        await Assert.That(allowed).IsTrue();
        await Assert.That(denied).IsFalse();
        await Assert.That(wildcard.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(wildcard.Kind).IsEqualTo(XsdComponentKind.Wildcard);
        await Assert.That(string.Join("|", log)).IsEqualTo("Allows:urn:a|Allows:urn:c");
    }

    [Test]
    public async Task Seam_Wildcard_Subset_AsksTheSupersetForEveryNamespace()
    {
        var log = new List<string>();
        var small = new FakeWildcard(log, XsdProcessContents.Strict, "urn:a");
        var large = new FakeWildcard(log, XsdProcessContents.Strict, "urn:a", "urn:b");

        var subset = small.IsSubsetOf(large);
        var superset = large.IsSubsetOf(small);

        await Assert.That(subset).IsTrue();
        await Assert.That(superset).IsFalse();
        await Assert
            .That(string.Join("|", log))
            .IsEqualTo("IsSubsetOf|Allows:urn:a|IsSubsetOf|Allows:urn:a|Allows:urn:b");
    }

    [Test]
    public async Task Seam_Wildcard_UnionAndIntersection_ReturnWildcards_OrNullWhenNotExpressible()
    {
        var log = new List<string>();
        var first = new FakeWildcard(log, XsdProcessContents.Strict, "urn:a", "urn:b");
        var second = new FakeWildcard(log, XsdProcessContents.Strict, "urn:b", "urn:c");
        var disjoint = new FakeWildcard(log, XsdProcessContents.Strict, "urn:z");

        var union = first.Union(second);
        var intersection = first.Intersect(second);
        var empty = first.Intersect(disjoint);

        await Assert.That(union).IsNotNull();
        await Assert.That(union!.Allows(new XPathQName("urn:c", "x"))).IsTrue();
        await Assert.That(intersection).IsNotNull();
        await Assert.That(intersection!.Allows(new XPathQName("urn:a", "x"))).IsFalse();
        await Assert.That(intersection.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(empty).IsNull();
    }
}
