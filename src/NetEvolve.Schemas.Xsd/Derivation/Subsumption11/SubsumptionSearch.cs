namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;

/// <summary>The search for a sequence of the restriction that the base does not accept. Compile-only shape; the search follows.</summary>
internal sealed class SubsumptionSearch
{
    private readonly SubsumptionSide _restriction;
    private readonly SubsumptionSide _base;
    private readonly XPathQName[] _symbols;
    private readonly Func<int, int, ParticleViolation> _correspond;
    private readonly int _maxWork;

    internal SubsumptionSearch(
        SubsumptionSide restriction,
        SubsumptionSide baseSide,
        XPathQName[] symbols,
        Func<int, int, ParticleViolation> correspond,
        int maxWork
    )
    {
        _restriction = restriction;
        _base = baseSide;
        _symbols = symbols;
        _correspond = correspond;
        _maxWork = maxWork;
    }

    internal int Work =>
        _restriction.Root.GetHashCode()
        + _base.Root.GetHashCode()
        + _symbols.Length
        + _maxWork
        + _correspond(0, 0).Message!.Length;
}
