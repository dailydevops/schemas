namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

/// <summary>The <c>role</c>, <c>flag</c>, and <c>severity</c> of one finding after inheritance and dynamic values; each is empty when none applies.</summary>
internal readonly struct ResolvedAnnotations
{
    internal ResolvedAnnotations(string role, string flags, string severity)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(severity);

        Role = role;
        Flags = flags;
        Severity = severity;
    }

    /// <summary>Gets the role from the assertion, else the rule, else the pattern or group.</summary>
    internal string Role { get; }

    /// <summary>Gets the flag tokens joined with one space, from the assertion, else the rule.</summary>
    internal string Flags { get; }

    /// <summary>Gets the raw severity token of the assertion; it never changes the severity of the issue.</summary>
    internal string Severity { get; }
}
