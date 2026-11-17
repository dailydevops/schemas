namespace NetEvolve.Schemas.Xsd.ContentModels.Automaton;

/// <summary>The kind of an <see cref="AutomatonParticle"/>.</summary>
internal enum AutomatonParticleKind
{
    Element = 0,
    Wildcard = 1,
    Sequence = 2,
    Choice = 3,
}
