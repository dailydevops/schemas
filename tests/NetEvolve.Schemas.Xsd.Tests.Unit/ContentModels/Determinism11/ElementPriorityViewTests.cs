namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism11;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism11;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

public class ElementPriorityViewTests
{
    private static readonly DocumentLocation Where = new("file:///a.xsd", 5, 2);

    private static NegatedWildcard Wildcard(string spec) =>
        NegatedWildcard.Create(NameConstraintTests.Spec(spec), XsdProcessContents.Skip, Where);

    [Test]
    public async Task Allows_NoName_ThoughTheWildcardDoes()
    {
        var wildcard = Wildcard("any");
        var view = new ElementPriorityView(wildcard);
        var name = new XPathQName("urn:test", "e");

        await Assert.That(wildcard.Allows(name)).IsTrue();
        await Assert.That(view.Allows(name)).IsFalse();
    }

    [Test]
    public async Task Properties_AreThoseOfTheWildcard()
    {
        var view = new ElementPriorityView(Wildcard("any"));

        await Assert.That(view.Kind).IsEqualTo(XsdComponentKind.Wildcard);
        await Assert.That(view.Name).IsEqualTo(default(XPathQName));
        await Assert.That(view.Location).IsEqualTo(Where);
        await Assert.That(view.ProcessContents).IsEqualTo(XsdProcessContents.Skip);
    }

    [Test]
    public async Task SetOperations_AreThoseOfTheWildcard()
    {
        var view = new ElementPriorityView(Wildcard("not a"));
        var other = Wildcard("not b");

        await Assert.That(view.IsSubsetOf(Wildcard("any"))).IsTrue();
        await Assert.That(view.IsSubsetOf(other)).IsFalse();
        await Assert.That(NegatedWildcard.ConstraintOf(view.Union(other)!)).IsEqualTo(NameConstraint.Any);
        await Assert
            .That(NegatedWildcard.ConstraintOf(view.Intersect(other)!))
            .IsEqualTo(NameConstraintTests.Spec("not a b"));
    }

    [Test]
    public async Task Create_WithoutAWildcard_Throws() =>
        await Assert.That(() => new ElementPriorityView(null!)).Throws<ArgumentNullException>();
}
