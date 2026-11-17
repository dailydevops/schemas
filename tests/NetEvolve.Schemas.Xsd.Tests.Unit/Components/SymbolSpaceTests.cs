namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class SymbolSpaceTests
{
    [Test]
    [Arguments("SimpleType", "Types")]
    [Arguments("ComplexType", "Types")]
    [Arguments("ElementDeclaration", "Elements")]
    [Arguments("AttributeDeclaration", "Attributes")]
    [Arguments("ModelGroupDefinition", "Groups")]
    [Arguments("AttributeGroup", "AttributeGroups")]
    [Arguments("IdentityConstraint", "IdentityConstraints")]
    [Arguments("Notation", "Notations")]
    public async Task Kind_MapsToItsSymbolSpace(string kind, string expected)
    {
        await Assert.That(XsdSymbolSpaces.TryOf(Enum.Parse<XsdComponentKind>(kind), out var space)).IsTrue();
        await Assert.That(space).IsEqualTo(Enum.Parse<XsdSymbolSpace>(expected));
    }

    [Test]
    [Arguments("Particle")]
    [Arguments("Wildcard")]
    [Arguments("AttributeUse")]
    [Arguments("ModelGroup")]
    public async Task Kind_WithoutSymbolSpace_IsNotNamed(string kind) =>
        await Assert.That(XsdSymbolSpaces.TryOf(Enum.Parse<XsdComponentKind>(kind), out _)).IsFalse();

    [Test]
    public async Task SameLocalName_InTwoSymbolSpaces_ResolvesIndependently()
    {
        var registry = NewRegistry(out var collector);
        var all = new[]
        {
            new RefComponent(XsdComponentKind.ElementDeclaration, "x", 1),
            new RefComponent(XsdComponentKind.AttributeDeclaration, "x", 2),
            new RefComponent(XsdComponentKind.ComplexType, "x", 3),
            new RefComponent(XsdComponentKind.ModelGroupDefinition, "x", 4),
            new RefComponent(XsdComponentKind.AttributeGroup, "x", 5),
            new RefComponent(XsdComponentKind.IdentityConstraint, "x", 6),
            new RefComponent(XsdComponentKind.Notation, "x", 7),
        };
        foreach (var c in all)
        {
            registry.Add(c);
        }

        await Assert.That(collector.Count).IsEqualTo(0);
        await Assert.That(registry.Count).IsEqualTo(7);
        foreach (var c in all)
        {
            await Assert.That(registry.TryGet(c.Kind, Name("x"), out var found)).IsTrue();
            await Assert.That(found).IsSameReferenceAs(c);
        }
    }

    [Test]
    public async Task SimpleAndComplexType_ShareOneSymbolSpace()
    {
        var registry = NewRegistry(out var collector);
        var complex = new RefComponent(XsdComponentKind.ComplexType, "t", 1);
        registry.Add(complex);
        registry.Add(new RefComponent(XsdComponentKind.SimpleType, "t", 2));

        await Assert.That(registry.TryGet(XsdComponentKind.SimpleType, Name("t"), out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(complex);
        await Assert.That(collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Add_AnonymousOrUnnamedKind_IsNotRegistered()
    {
        var registry = NewRegistry(out _);
        registry.Add(new RefComponent(XsdComponentKind.Particle, "p", 1));
        registry.Add(new UnnamedComponent());

        await Assert.That(registry.Count).IsEqualTo(0);
    }
}
