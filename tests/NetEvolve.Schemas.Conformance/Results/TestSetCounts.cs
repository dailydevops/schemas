namespace NetEvolve.Schemas.Conformance.Results;

using System.Runtime.InteropServices;

/// <summary>Case counts of a test set or a whole suite; skipped cases have their own bucket.</summary>
/// <param name="Passed">The number of passed cases.</param>
/// <param name="Failed">The number of failed cases, known failures included.</param>
/// <param name="Skipped">The number of skipped cases.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct TestSetCounts(int Passed, int Failed, int Skipped)
{
    /// <summary>Gets the number of applicable cases, which excludes skipped cases.</summary>
    public int Applicable => Passed + Failed;

    /// <summary>Gets the number of all cases.</summary>
    public int Total => Applicable + Skipped;

    /// <summary>Gets the share of applicable cases that passed; <c>1</c> when no case is applicable.</summary>
    public double PassRate => Applicable == 0 ? 1d : (double)Passed / Applicable;
}
