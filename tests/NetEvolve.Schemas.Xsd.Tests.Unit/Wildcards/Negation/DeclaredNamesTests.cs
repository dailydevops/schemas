namespace NetEvolve.Schemas.Xsd.Tests.Unit.Wildcards.Negation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.Substitution;
using NetEvolve.Schemas.Xsd.Wildcards.Core;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

public class DeclaredNamesTests
{
    private static readonly DocumentLocation Where = new("file:///a.xsd", 3, 1);

    private static XPathQName Name(string local) => new("urn:test", local);

    private static Particle Element(string local) =>
        Particle.Create(Where, 1, 1, new FakeComponent(XsdComponentKind.ElementDeclaration, local));

    private static Particle Group(ModelGroupCompositor compositor, params Particle[] particles) =>
        Particle.Create(Where, 1, 1, ModelGroup.Create(compositor, Where, particles));

    [Test]
    public async Task Of_Names_ContainsExactlyThose()
    {
        var names = DeclaredNames.Of([Name("a"), Name("b")]);

        await Assert.That(names.Contains(Name("a"))).IsTrue();
        await Assert.That(names.Contains(Name("b"))).IsTrue();
        await Assert.That(names.Contains(Name("c"))).IsFalse();
        await Assert.That(names.Contains(new XPathQName("urn:other", "a"))).IsFalse();
    }

    [Test]
    public async Task Of_Names_DoesNotFollowLaterChangesOfTheList()
    {
        var list = new List<XPathQName> { Name("a") };
        var names = DeclaredNames.Of(list);

        list.Add(Name("b"));

        await Assert.That(names.Contains(Name("b"))).IsFalse();
    }

    [Test]
    public async Task Of_Registry_AsksTheRegistryForTheKindWhenQueried()
    {
        var registry = new FakeComponentRegistry([]);
        var elements = DeclaredNames.Of(registry, XsdComponentKind.ElementDeclaration);
        var before = elements.Contains(Name("x"));

        registry.Add(new FakeComponent(XsdComponentKind.ElementDeclaration, "x"));
        registry.Add(new FakeComponent(XsdComponentKind.AttributeDeclaration, "y"));

        await Assert.That(before).IsFalse();
        await Assert.That(elements.Contains(Name("x"))).IsTrue();
        await Assert.That(elements.Contains(Name("y"))).IsFalse();
        await Assert
            .That(DeclaredNames.Of(registry, XsdComponentKind.AttributeDeclaration).Contains(Name("y")))
            .IsTrue();
    }

    [Test]
    public async Task SiblingsOf_ListsTheElementsOfTheWholeContentModel()
    {
        var content = Group(
            ModelGroupCompositor.Sequence,
            Element("a"),
            Group(ModelGroupCompositor.Choice, Element("b"), Element("c")),
            Particle.Create(Where, 1, 1, NegatedWildcard.Create(NameConstraint.Any, XsdProcessContents.Lax, Where))
        );

        var siblings = DeclaredNames.SiblingsOf(content);

        await Assert.That(siblings.Contains(Name("a"))).IsTrue();
        await Assert.That(siblings.Contains(Name("b"))).IsTrue();
        await Assert.That(siblings.Contains(Name("c"))).IsTrue();
        await Assert.That(siblings.Contains(Name("d"))).IsFalse();
    }

    [Test]
    public async Task SiblingsOf_FollowsAGroupDefinition_AndSkipsAnUnresolvedReference()
    {
        var definition = ModelGroupDefinition.Create(
            Name("g"),
            Where,
            ModelGroup.Create(ModelGroupCompositor.Sequence, Where, [Element("inner")])
        );
        var content = Group(
            ModelGroupCompositor.Sequence,
            Particle.Create(Where, 1, 1, definition),
            Particle.CreateReference(Where, 1, 1, XsdSymbolSpace.Elements, Name("unresolved"))
        );

        var siblings = DeclaredNames.SiblingsOf(content);

        await Assert.That(siblings.Contains(Name("inner"))).IsTrue();
        await Assert.That(siblings.Contains(Name("unresolved"))).IsFalse();
    }

    [Test]
    public async Task SiblingsOf_ACircularGroupReference_Terminates()
    {
        var group = ModelGroup.Create(
            ModelGroupCompositor.Sequence,
            Where,
            [Element("a"), Particle.CreateReference(Where, 1, 1, XsdSymbolSpace.Groups, Name("g"))]
        );
        var definition = ModelGroupDefinition.Create(Name("g"), Where, group);
        group.Bind(0, definition);

        var siblings = DeclaredNames.SiblingsOf(Particle.Create(Where, 1, 1, definition));

        await Assert.That(siblings.Contains(Name("a"))).IsTrue();
    }

    [Test]
    public async Task SiblingsOf_WithSubstitutionGroups_ListsTheMembersOfEachDeclaration()
    {
        var type = new FakeType("t");
        var head = new FakeElement("head", type);
        var member = new FakeElement("member", type, head);
        var deep = new FakeElement("deep", type, member);
        var groups = SubstitutionGroups.Build(
            [head, member, deep],
            new ChainDerivationCheck(),
            new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default))
        );
        var content = Group(ModelGroupCompositor.Sequence, Particle.Create(Where, 1, 1, head), Element("plain"));

        var implicitly = DeclaredNames.SiblingsOf(content, groups);
        var directly = DeclaredNames.SiblingsOf(content);

        await Assert.That(implicitly.Contains(Name("head"))).IsTrue();
        await Assert.That(implicitly.Contains(Name("member"))).IsTrue();
        await Assert.That(implicitly.Contains(Name("deep"))).IsTrue();
        await Assert.That(implicitly.Contains(Name("plain"))).IsTrue();
        await Assert.That(directly.Contains(Name("head"))).IsTrue();
        await Assert.That(directly.Contains(Name("member"))).IsFalse();
    }

    [Test]
    public async Task SiblingsOf_NoContent_IsEmpty() =>
        await Assert.That(DeclaredNames.SiblingsOf(null).Contains(Name("a"))).IsFalse();

    [Test]
    public async Task Factories_RejectNullArguments()
    {
        await Assert.That(() => DeclaredNames.Of((IReadOnlyList<XPathQName>)null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => DeclaredNames.Of(null!, XsdComponentKind.ElementDeclaration))
            .Throws<ArgumentNullException>();
    }
}
