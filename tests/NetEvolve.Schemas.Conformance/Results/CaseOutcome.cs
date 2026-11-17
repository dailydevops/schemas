namespace NetEvolve.Schemas.Conformance.Results;

/// <summary>The outcome of one conformance case.</summary>
public enum CaseOutcome
{
    /// <summary>The case produced the expected result.</summary>
    Pass,

    /// <summary>The case did not produce the expected result.</summary>
    Fail,

    /// <summary>The case was excluded from the run; it is not applicable.</summary>
    Skipped,
}
