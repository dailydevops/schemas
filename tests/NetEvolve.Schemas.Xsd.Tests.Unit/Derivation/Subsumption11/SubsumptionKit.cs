namespace NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Subsumption11;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Subsumption11;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Tests.Unit.Derivation.Particles10;

/// <summary>Builds the checks and the assertions of the subsumption tests; the particles come from the kit of the 1.0 table.</summary>
internal static class SubsumptionKit
{
    public const long Billion = 1_000_000_000;

    public static ContentSubsumption Subsumption(int maxWork = ContentSubsumption.DefaultMaxWork) =>
        new(new TypeDerivationOk(XsdVersion.V11), ParticleKit.Values(XsdVersion.V11), maxWork);

    public static SubsumptionOutcome Check(Particle restriction, Particle baseParticle) =>
        Subsumption().Check(restriction, baseParticle);

    /// <summary>The verdict of the XSD 1.0 table, which compares the structure of the two particles.</summary>
    public static bool TableAccepts(Particle restriction, Particle baseParticle) =>
        !ParticleKit.Checker().Check(restriction, baseParticle).IsViolation;

    public static async Task AssertSubsumed(Particle restriction, Particle baseParticle)
    {
        var outcome = Check(restriction, baseParticle);

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.Subsumed);
        await Assert.That(outcome.Violation.IsViolation).IsFalse();
    }

    public static async Task AssertNotSubsumed(
        Particle restriction,
        Particle baseParticle,
        string clause = "cos-content-act-restrict.1",
        int? line = null
    )
    {
        var outcome = Check(restriction, baseParticle);

        await Assert.That(outcome.Verdict).IsEqualTo(SubsumptionVerdict.NotSubsumed);
        await Assert.That(outcome.Violation.IsViolation).IsTrue();
        await Assert.That(outcome.Violation.Code).IsEqualTo(XsdRuleCodes.CosContentActRestrict);
        await Assert.That(outcome.Violation.Clause).IsEqualTo(clause);
        await Assert.That(outcome.Violation.Message).IsNotEmpty();
        if (line is { } expected)
        {
            await Assert.That(outcome.Violation.Location.Line).IsEqualTo(expected);
        }
    }
}
