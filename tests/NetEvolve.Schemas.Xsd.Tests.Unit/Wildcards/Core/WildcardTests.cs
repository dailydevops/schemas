namespace NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Core;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

public class WildcardTests
{
    private static readonly DocumentLocation Where = new("file:///a.xsd", 7, 4);

    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);

    private XsdIssueReporter Reporter => new(_collector);

    private static Wildcard Create(string? ns, string? process = null)
    {
        var created = Wildcard.TryCreate(ns, process, "urn:t", Where, out var wildcard);
        if (!created)
        {
            throw new InvalidOperationException("Not a wildcard.");
        }

        return wildcard!;
    }

    [Test]
    [Arguments(null, "Strict")]
    [Arguments("strict", "Strict")]
    [Arguments("lax", "Lax")]
    [Arguments("skip", "Skip")]
    public async Task TryCreate_ParsesProcessContents(string? value, string expected)
    {
        var wildcard = Create("##any", value);

        await Assert.That(wildcard.ProcessContents).IsEqualTo(Enum.Parse<XsdProcessContents>(expected));
        await Assert.That(wildcard.Kind).IsEqualTo(XsdComponentKind.Wildcard);
        await Assert.That(wildcard.Name).IsEqualTo(default(XPathQName));
        await Assert.That(wildcard.Location).IsEqualTo(Where);
    }

    [Test]
    [Arguments("Strict")]
    [Arguments("")]
    [Arguments("other")]
    public async Task TryCreate_InvalidProcessContents_Fails(string value)
    {
        var created = Wildcard.TryCreate("##any", value, "urn:t", Where, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
    }

    [Test]
    public async Task TryCreate_InvalidNamespace_Fails()
    {
        var created = Wildcard.TryCreate("##any ##local", null, "urn:t", Where, out var wildcard);

        await Assert.That(created).IsFalse();
        await Assert.That(wildcard).IsNull();
    }

    [Test]
    public async Task TryCreate_MissingNamespace_DefaultsToAny()
    {
        var wildcard = Create(null);

        await Assert.That(wildcard.Constraint.ToString()).IsEqualTo("##any");
    }

    [Test]
    [Arguments("##other", "urn:t", false)]
    [Arguments("##other", "urn:o", true)]
    [Arguments("##other", "", false)]
    [Arguments("##local", "", true)]
    [Arguments("##local", "urn:o", false)]
    [Arguments("##targetNamespace", "urn:t", true)]
    [Arguments("##targetNamespace", "urn:o", false)]
    public async Task Allows_TestsTheNamespaceOfTheExpandedName(string ns, string nameNs, bool expected) =>
        await Assert.That(Create(ns).Allows(new XPathQName(nameNs, "x"))).IsEqualTo(expected);

    [Test]
    public async Task Union_KeepsTheProcessContentsOfTheFirstWildcard()
    {
        var first = Create("urn:a", "lax");
        var second = Create("urn:b", "skip");

        var union = first.Union(second);

        await Assert.That(union).IsNotNull();
        await Assert.That(union!.ProcessContents).IsEqualTo(XsdProcessContents.Lax);
        await Assert.That(union.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(union.Allows(new XPathQName("urn:c", "x"))).IsFalse();
    }

    [Test]
    public async Task Intersect_KeepsTheProcessContentsOfTheFirstWildcard()
    {
        var first = Create("urn:a urn:b", "skip");
        var second = Create("urn:b urn:c", "strict");

        var intersection = first.Intersect(second);

        await Assert.That(intersection!.ProcessContents).IsEqualTo(XsdProcessContents.Skip);
        await Assert.That(intersection.Allows(new XPathQName("urn:b", "x"))).IsTrue();
        await Assert.That(intersection.Allows(new XPathQName("urn:a", "x"))).IsFalse();
    }

    [Test]
    public async Task IsSubsetOf_DelegatesToTheConstraints()
    {
        await Assert.That(Create("urn:a").IsSubsetOf(Create("##other"))).IsTrue();
        await Assert.That(Create("##other").IsSubsetOf(Create("urn:a"))).IsFalse();
    }

    [Test]
    public async Task Union_NotExpressible_ReturnsNullAndReportsAttributeWildcardUnion()
    {
        var union = Create("##other").Union(Create("##local"), Reporter);

        await Assert.That(union).IsNull();
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosAwUnion);
        await Assert.That(issues[0].Location.Line).IsEqualTo(7);
    }

    [Test]
    public async Task Intersect_NotExpressible_ReturnsNullAndReportsAttributeWildcardIntersection()
    {
        var left = Wildcard.Create(
            NamespaceConstraintTests.Parse("##other", "urn:t"),
            XsdProcessContents.Strict,
            Where
        );
        var right = Wildcard.Create(
            NamespaceConstraintTests.Parse("##other", "urn:o"),
            XsdProcessContents.Strict,
            Where
        );

        var intersection = left.Intersect(right, Reporter);

        await Assert.That(intersection).IsNull();
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.CosAwIntersect);
    }

    [Test]
    public async Task Union_Expressible_ReportsNothing()
    {
        var union = Create("##other").Union(Create("urn:a"), Reporter);

        await Assert.That(union).IsNotNull();
        await Assert.That(_collector.ToResult().Issues).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Operations_WithNullArgument_Throw()
    {
        var wildcard = Create("##any");

        await Assert.That(() => wildcard.Union(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.Intersect(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.IsSubsetOf(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => wildcard.Union(wildcard, null!)).Throws<ArgumentNullException>();
    }
}
