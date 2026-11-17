namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;

/// <summary>The particle a child element matched.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct MatchedParticle
{
    internal MatchedParticle(int particleIndex, bool isWildcard)
    {
        ParticleIndex = particleIndex;
        IsWildcard = isWildcard;
    }

    /// <summary>Gets the index of the particle in the program.</summary>
    public int ParticleIndex { get; }

    /// <summary>Gets a value indicating whether the particle is a wildcard.</summary>
    public bool IsWildcard { get; }
}
