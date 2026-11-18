namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

/// <summary>What evaluating one assertion for one context node produced.</summary>
internal enum AssertionOutcome : byte
{
    /// <summary>The assertion did not fire: a true assert test or a false report test.</summary>
    Silent = 0,

    /// <summary>The assertion fired and its finding was delivered.</summary>
    Fired = 1,

    /// <summary>The test could not be evaluated; the problem was reported and no finding exists.</summary>
    Problem = 2,
}
