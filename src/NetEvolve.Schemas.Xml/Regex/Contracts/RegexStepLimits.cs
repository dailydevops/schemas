namespace NetEvolve.Schemas.Xml.Regex.Contracts;

using NetEvolve.Arguments;

/// <summary>
/// Immutable step ceilings of a match, the denial-of-service bound of the matcher. One step is one instruction executed by one
/// thread at one input position, so the same input aborts identically on every target.
/// </summary>
/// <remarks>Limitation: the defaults are the placeholders of the approved design; DT-11 sets them from benchmarks. A limit of zero never disables a ceiling.</remarks>
public sealed class RegexStepLimits
{
    private RegexStepLimits(long maxNfaSteps, long maxBacktrackSteps)
    {
        MaxNfaSteps = maxNfaSteps;
        MaxBacktrackSteps = maxBacktrackSteps;
    }

    /// <summary>Gets the default limits.</summary>
    public static RegexStepLimits Default { get; } = new(100_000_000, 1_000_000);

    /// <summary>Gets the ceiling for patterns without back references, which run on a simulated automaton; the default is 100,000,000.</summary>
    public long MaxNfaSteps { get; }

    /// <summary>Gets the ceiling for patterns with back references, which run on a backtracking machine; the default is 1,000,000.</summary>
    public long MaxBacktrackSteps { get; }

    /// <summary>Returns a copy with another <see cref="MaxNfaSteps"/>.</summary>
    /// <param name="value">The new value, one or greater.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public RegexStepLimits WithMaxNfaSteps(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(value, MaxBacktrackSteps);
    }

    /// <summary>Returns a copy with another <see cref="MaxBacktrackSteps"/>.</summary>
    /// <param name="value">The new value, one or greater.</param>
    /// <returns>The new limits.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is below one.</exception>
    public RegexStepLimits WithMaxBacktrackSteps(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1L);

        return new(MaxNfaSteps, value);
    }
}
