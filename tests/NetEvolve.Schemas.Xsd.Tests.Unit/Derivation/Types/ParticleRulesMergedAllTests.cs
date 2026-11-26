namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Types;

using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>Particle Valid (Extension) for the merged <c>all</c> group of XSD 1.1 (Structures 3.4.2 clause 4.2.3.2).</summary>
public class ParticleRulesMergedAllTests
{
    private static Particle All(long max, params Particle[] particles) =>
        TypeFixture.Group(ModelGroupCompositor.All, 1, max, particles);

    [Test]
    public async Task IsValidExtension_AllGroupWithTheBaseMembersFirst_IsValid()
    {
        var a = TypeFixture.Element("a");
        var merged = All(1, a, TypeFixture.Element("b"));

        await Assert.That(ParticleRules.IsValidExtension(merged, All(1, a))).IsTrue();
    }

    [Test]
    public async Task IsValidExtension_AllGroupWithAnotherMemberFirst_IsInvalid()
    {
        var merged = All(1, TypeFixture.Element("b"), TypeFixture.Element("a"));

        await Assert.That(ParticleRules.IsValidExtension(merged, All(1, TypeFixture.Element("a")))).IsFalse();
    }

    [Test]
    public async Task IsValidExtension_AllGroupWithFewerMembersThanTheBase_IsInvalid()
    {
        var merged = All(1, TypeFixture.Element("a"));

        await Assert
            .That(ParticleRules.IsValidExtension(merged, All(1, TypeFixture.Element("a"), TypeFixture.Element("b"))))
            .IsFalse();
    }

    [Test]
    public async Task IsValidExtension_RepeatingAllGroup_IsInvalid()
    {
        var a = TypeFixture.Element("a");

        await Assert.That(ParticleRules.IsValidExtension(All(2, a), All(1, a))).IsFalse();
    }

    [Test]
    public async Task IsValidExtension_AllGroupOverASequenceBase_IsInvalid()
    {
        var a = TypeFixture.Element("a");

        await Assert.That(ParticleRules.IsValidExtension(All(1, a), TypeFixture.Sequence(a))).IsFalse();
    }

    [Test]
    public async Task IsValidExtension_AllGroupOverAGroupDefinitionBase_IsValid()
    {
        var a = TypeFixture.Element("a");
        var definition = ModelGroupDefinition.Create(
            new NetEvolve.Schemas.XPath.Api.XPathQName("urn:test", "g"),
            TypeFixture.Where,
            (ModelGroup)All(1, a).Term!
        );
        var baseParticle = Particle.Create(TypeFixture.Where, 1, 1, definition);

        await Assert.That(ParticleRules.IsValidExtension(All(1, a, TypeFixture.Element("b")), baseParticle)).IsTrue();
    }
}
