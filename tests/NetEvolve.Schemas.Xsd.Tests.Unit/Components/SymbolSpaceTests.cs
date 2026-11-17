namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class SymbolSpaceTests
{
    [Test]
    [Arguments(XsdComponentKind.SimpleType, XsdSymbolSpace.Types)]
    [Arguments(XsdComponentKind.ComplexType, XsdSymbolSpace.Types)]
    [Arguments(XsdComponentKind.ElementDeclaration, XsdSymbolSpace.Elements)]
    [Arguments(XsdComponentKind.AttributeDeclaration, XsdSymbolSpace.Attributes)]
    [Arguments(XsdComponentKind.ModelGroupDefinition, XsdSymbolSpace.Groups)]
    [Arguments(XsdComponentKind.AttributeGroup, XsdSymbolSpace.AttributeGroups)]
    [Arguments(XsdComponentKind.IdentityConstraint, XsdSymbolSpace.IdentityConstraints)]
    [Arguments(XsdComponentKind.Notation, XsdSymbolSpace.Notations)]
    public async Task Kind_MapsToItsSymbolSpace(XsdComponentKind kind, XsdSymbolSpace expected)
    {
        await Assert.That(XsdSymbolSpaces.TryOf(kind, out var space)).IsTrue();
        await Assert.That(space).IsEqualTo(expected);
    }

    [Test]
    [Arguments(XsdComponentKind.Particle)]
    [Arguments(XsdComponentKind.Wildcard)]
    [Arguments(XsdComponentKind.AttributeUse)]
    [Arguments(XsdComponentKind.ModelGroup)]
    public async Task Kind_WithoutSymbolSpace_IsNotNamed(XsdComponentKind kind) =>
        await Assert.That(XsdSymbolSpaces.TryOf(kind, out _)).IsFalse();

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
