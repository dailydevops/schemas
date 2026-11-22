namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>A violated constraint of a type derivation: the rule, the numbered clause, and a message; <see langword="default"/> is none.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct TypeViolation
{
    internal TypeViolation(DiagnosticCode code, string clause, string message)
    {
        ArgumentNullException.ThrowIfNull(clause);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Clause = clause;
        Message = message;
    }

    /// <summary>Gets the result of a derivation without a violation.</summary>
    public static TypeViolation None => default;

    /// <summary>Gets a value indicating whether a constraint is violated.</summary>
    public bool IsViolation => Message is not null;

    /// <summary>Gets the code of the violated rule.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the numbered clause, for example <c>cos-ct-extends.1.2</c>; its prefix is the id of the rule.</summary>
    public string? Clause { get; }

    /// <summary>Gets the message, <see langword="null"/> without a violation.</summary>
    public string? Message { get; }
}
