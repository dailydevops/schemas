namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

/// <summary>How far the construction of a <see cref="SubsumptionSide"/> got.</summary>
internal enum SideStatus
{
    /// <summary>The content model is compiled and can be compared.</summary>
    Ready = 0,

    /// <summary>The content model has a term that is not resolved, or an all group below the content model; it is not compared.</summary>
    NotComparable = 1,

    /// <summary>The content model exceeds a ceiling of the automaton or of the counters.</summary>
    TooLarge = 2,
}
