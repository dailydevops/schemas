namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

/// <summary>The kind of result of a subsumption check.</summary>
internal enum SubsumptionVerdict
{
    /// <summary>Every sequence of the restriction is accepted by the base, and the bindings correspond.</summary>
    Subsumed = 0,

    /// <summary>The restriction accepts a sequence the base does not, or an element does not correspond.</summary>
    NotSubsumed = 1,

    /// <summary>The content models are not compared: a term is not resolved, a group is circular, or one of them is an all group.</summary>
    NotCompared = 2,

    /// <summary>The check ran into its work limit or a ceiling of the content model before it could decide.</summary>
    Undecided = 3,
}
