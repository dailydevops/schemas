namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

/// <summary>
/// The <c>role</c>, <c>flag</c>, and <c>severity</c> attributes of one assertion, rule, pattern, or group; an attribute the element
/// does not carry is <see langword="null"/>. A rule has no <c>severity</c>, a pattern or group has neither <c>flag</c> nor
/// <c>severity</c>. Immutable and shared by every run.
/// </summary>
internal sealed class CompiledAnnotations
{
    /// <summary>Gets the annotations of an element that carries none of the three attributes.</summary>
    internal static readonly CompiledAnnotations None = new(null, null, null);

    internal CompiledAnnotations(CompiledAnnotation? role, CompiledAnnotation? flags, CompiledAnnotation? severity)
    {
        Role = role;
        Flags = flags;
        Severity = severity;
    }

    internal CompiledAnnotation? Role { get; }

    internal CompiledAnnotation? Flags { get; }

    internal CompiledAnnotation? Severity { get; }

    /// <summary>Gets the static role for an event; a dynamic role is left empty because the events are delivered before any assertion fires.</summary>
    internal string StaticRole => Role?.Text ?? string.Empty;

    /// <summary>Gets the static flags for an event, tokens joined with one space; a dynamic value is left empty.</summary>
    internal string StaticFlags => Flags?.Text ?? string.Empty;
}
