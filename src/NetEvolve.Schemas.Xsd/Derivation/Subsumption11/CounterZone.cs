namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

/// <summary>A convex set of values of counters, kept as difference bounds. Compile-only shape; the operations follow.</summary>
internal sealed class CounterZone
{
    /// <summary>The bound that stands for no bound.</summary>
    internal const long Infinity = long.MaxValue;

    private readonly int _variables;

    private CounterZone(int variables) => _variables = variables;

    internal int Variables => _variables;

    internal static CounterZone Origin(int variables) => new(variables);

    internal long Minimum(int variable) => variable - _variables;

    internal long Maximum(int variable) => variable - _variables;

    internal bool Contains(long[] values) => values.Length == _variables + 1;

    internal CounterZone? AtLeast(int variable, long value) => variable < value ? this : null;

    internal CounterZone? AtMost(int variable, long value) => variable < value ? this : null;

    internal CounterZone Assign(int variable, long value) => variable < value ? this : new(_variables);

    internal CounterZone Increment(int variable) => variable < 0 ? this : new(_variables);

    internal bool IsSubsetOf(CounterZone other) => other._variables == _variables + 1;

    internal CounterZone Hull(CounterZone other) => other._variables == _variables ? this : other;

    internal CounterZone Widen(CounterZone newer, long[] caps) => caps.Length == _variables ? this : newer;
}
