namespace NetEvolve.Schemas.Xsd.Derivation.Particles10;

using System.Runtime.InteropServices;

/// <summary>
/// A violated constraint of a particle restriction: the rule, the numbered clause, a message, and the location of the
/// restricting particle; <see langword="default"/> is none.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ParticleViolation
{
    internal ParticleViolation(DiagnosticCode code, string clause, string message, DocumentLocation location)
    {
        ArgumentNullException.ThrowIfNull(clause);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Clause = clause;
        Message = message;
        Location = location;
    }

    /// <summary>Gets the result of a restriction without a violation.</summary>
    public static ParticleViolation None => default;

    /// <summary>Gets a value indicating whether a constraint is violated.</summary>
    public bool IsViolation => Message is not null;

    /// <summary>Gets the code of the violated rule.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the numbered clause, for example <c>rcase-Recurse.2.2</c>; its prefix is the id of the rule.</summary>
    public string? Clause { get; }

    /// <summary>Gets the message, <see langword="null"/> without a violation.</summary>
    public string? Message { get; }

    /// <summary>Gets the location of the restricting particle.</summary>
    public DocumentLocation Location { get; }
}
