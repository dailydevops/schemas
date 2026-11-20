namespace NetEvolve.Schemas.Xsd.Derivation.Particles10;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>Particle Valid (Restriction) of Structures 1.0, 3.9.6 (not implemented yet).</summary>
internal sealed class ParticleRestriction
{
    private readonly TypeDerivationOk _derivation;

    internal ParticleRestriction(TypeDerivationOk derivation)
    {
        Argument.ThrowIfNull(derivation);

        _derivation = derivation;
    }

    internal ParticleViolation Check(Particle restriction, Particle baseParticle)
    {
        Argument.ThrowIfNull(restriction);
        Argument.ThrowIfNull(baseParticle);

        return _derivation is null ? default : ParticleViolation.None;
    }

    internal void Report(Particle restriction, Particle baseParticle, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(reporter);

        _ = Check(restriction, baseParticle);
    }
}
