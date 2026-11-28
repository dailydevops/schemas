namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;

/// <summary>The outcome of a derivation check.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct DerivationResult
{
    private DerivationResult(XsdRule? violatedRule) => ViolatedRule = violatedRule;

    /// <summary>Gets the result of a valid derivation.</summary>
    public static DerivationResult Valid => default;

    /// <summary>Gets a value indicating whether the derivation is valid.</summary>
    public bool IsValid => ViolatedRule is null;

    /// <summary>Gets the violated rule, <see langword="null"/> when the derivation is valid.</summary>
    public XsdRule? ViolatedRule { get; }

    /// <summary>Creates an invalid result.</summary>
    /// <param name="rule">The violated rule.</param>
    /// <returns>The result.</returns>
    public static DerivationResult Invalid(XsdRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new(rule);
    }
}
