namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10.ParticleKit;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11.SubsumptionKit;

/// <summary>
/// Restrictions that the table of XSD 1.0 and the language inclusion of XSD 1.1 judge differently, each with the verdict of both:
/// the table compares the structure of the particles, the inclusion compares the words they accept.
/// </summary>
public class ContentSubsumptionVersionTests
{
    [Test]
    public async Task SequenceOfChoices_AgainstAChoiceOfSequences_IsValidIn11AndInvalidIn10()
    {
        var restriction = Seq([Cho([El("a"), El("b")]), El("c")]);
        var baseParticle = Cho([Seq([El("a"), El("c")]), Seq([El("b"), El("c")])]);

        await AssertSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsFalse();
    }

    [Test]
    public async Task SequenceOfTwoElements_AgainstTheElementWithARange_IsValidIn11AndInvalidIn10()
    {
        var restriction = Seq([El("a"), El("a")]);
        var baseParticle = El("a", 0, 2);

        await AssertSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsFalse();
    }

    [Test]
    public async Task UnrolledSequence_AgainstTheRepeatedSequence_IsValidIn11AndInvalidIn10()
    {
        var restriction = Seq([El("a"), El("b"), El("a"), El("b")]);
        var baseParticle = Seq([El("a"), El("b")], 0, 2);

        await AssertSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsFalse();
    }

    [Test]
    public async Task ElementAgainstAStrictWildcard_IsInvalidIn11AndValidIn10()
    {
        // The binding of the base is strict, which no element declaration subsumes (clause 2); the table checks the namespace only.
        var restriction = El("a");
        var baseParticle = Any(process: XsdProcessContents.Strict);

        await AssertNotSubsumed(restriction, baseParticle, "cos-content-act-restrict.2.subsumes.3");
        await Assert.That(TableAccepts(restriction, baseParticle)).IsTrue();
    }

    [Test]
    public async Task ElementAgainstALaxWildcard_IsValidInBoth()
    {
        var restriction = El("a");
        var baseParticle = Any();

        await AssertSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsTrue();
    }

    [Test]
    public async Task RestrictionThatDropsAnAlternative_IsValidInBoth()
    {
        var restriction = Cho([El("a"), El("b")]);
        var baseParticle = Cho([El("a"), El("b"), El("c")]);

        await AssertSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsTrue();
    }

    [Test]
    public async Task RestrictionThatAddsAnAlternative_IsInvalidInBoth()
    {
        var restriction = Cho([El("a"), El("b"), El("c")]);
        var baseParticle = Cho([El("a"), El("b")]);

        await AssertNotSubsumed(restriction, baseParticle);
        await Assert.That(TableAccepts(restriction, baseParticle)).IsFalse();
    }
}
