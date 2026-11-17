namespace NetEvolve.Schemas.XPath.Api;

/// <summary>The outcome of an evaluation.</summary>
public enum XPathEvaluationStatus
{
    /// <summary>The evaluation produced a value.</summary>
    Ok = 0,

    /// <summary>The evaluation failed; the diagnostic says why.</summary>
    Error = 1,
}
