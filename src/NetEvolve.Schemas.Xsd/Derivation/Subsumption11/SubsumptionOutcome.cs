namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;

/// <summary>The result of a subsumption check: the verdict, the violation that goes with it, and the work it took.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct SubsumptionOutcome
{
    internal SubsumptionOutcome(SubsumptionVerdict verdict, ParticleViolation violation, int work)
    {
        Verdict = verdict;
        Violation = violation;
        Work = work;
    }

    /// <summary>Gets the verdict.</summary>
    public SubsumptionVerdict Verdict { get; }

    /// <summary>Gets the violation of a <see cref="SubsumptionVerdict.NotSubsumed"/> or <see cref="SubsumptionVerdict.Undecided"/> verdict, none otherwise.</summary>
    public ParticleViolation Violation { get; }

    /// <summary>Gets the units of work the search took; it grows with the content models, never with their occurrence bounds.</summary>
    public int Work { get; }
}
