namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// Content type restricts (Complex Content) (<c>cos-content-act-restrict</c>, Structures 1.1, 3.4.6.4). Compile-only shape; the
/// check follows.
/// </summary>
internal sealed class ContentSubsumption
{
    /// <summary>The work limit of one check.</summary>
    internal const int DefaultMaxWork = 200_000;

    private readonly TypeDerivationOk _derivation;
    private readonly int _maxWork;

    internal ContentSubsumption(TypeDerivationOk derivation, int maxWork = DefaultMaxWork)
    {
        _derivation = derivation;
        _maxWork = maxWork;
    }

    internal SubsumptionOutcome Check(Particle restriction, Particle baseParticle) =>
        new(
            SubsumptionVerdict.Subsumed,
            ParticleViolation.None,
            _derivation is null || restriction is null || baseParticle is null ? 0 : _maxWork - _maxWork
        );

    internal void Report(Particle restriction, Particle baseParticle, XsdIssueReporter reporter) =>
        _ = Check(restriction, baseParticle).Work + reporter.GetHashCode();
}
