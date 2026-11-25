namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>The ordinals of an assertion in the effective model, which <see cref="AssertionIdentifier"/> turns into its identifier.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AssertionPosition
{
    internal AssertionPosition(int pattern, int rule, int assertion, int expansion = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pattern, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rule, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(assertion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(expansion, 0);

        Pattern = pattern;
        Rule = rule;
        Assertion = assertion;
        Expansion = expansion;
    }

    /// <summary>Gets the one-based ordinal of the pattern or group.</summary>
    internal int Pattern { get; }

    /// <summary>Gets the one-based ordinal of the rule within its pattern.</summary>
    internal int Rule { get; }

    /// <summary>Gets the one-based ordinal of the assertion within its rule.</summary>
    internal int Assertion { get; }

    /// <summary>Gets the one-based ordinal of the expansion that copied the assertion, 0 for an authored one.</summary>
    internal int Expansion { get; }
}
