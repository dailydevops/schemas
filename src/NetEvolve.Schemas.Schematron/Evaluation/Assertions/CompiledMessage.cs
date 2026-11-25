namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Arguments;

/// <summary>
/// The rich text of an assertion, a diagnostic, or a property, compiled once in the scope of its rule. The inline elements
/// <c>emph</c>, <c>dir</c>, and <c>span</c> stay in the structure; building the text contributes their content without markup.
/// </summary>
internal sealed class CompiledMessage
{
    internal CompiledMessage(
        string id,
        DocumentLocation location,
        MessagePart[] parts,
        string? language = null,
        string? role = null,
        string? scheme = null,
        string? fpi = null,
        string? icon = null,
        string? see = null
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(parts);

        Id = id;
        Location = location;
        Parts = parts;
        Language = language ?? string.Empty;
        Role = role ?? string.Empty;
        Scheme = scheme ?? string.Empty;
        Fpi = fpi ?? string.Empty;
        Icon = icon ?? string.Empty;
        See = see ?? string.Empty;
        for (var i = 0; i < parts.Length; i++)
        {
            HasCopyOf |= parts[i].Kind == MessagePartKind.CopyOf;
        }
    }

    /// <summary>Gets the id of the diagnostic or property, empty for the message of the assertion itself.</summary>
    internal string Id { get; }

    internal DocumentLocation Location { get; }

    internal MessagePart[] Parts { get; }

    /// <summary>Gets the language in effect for the message, empty when none.</summary>
    internal string Language { get; }

    /// <summary>Gets the <c>role</c> of the property, empty when it has none (and for a diagnostic or an assertion).</summary>
    internal string Role { get; }

    /// <summary>Gets the <c>scheme</c> of the property, empty when it has none.</summary>
    internal string Scheme { get; }

    /// <summary>Gets the <c>fpi</c> of the diagnostic or property, empty when it has none (and for an assertion).</summary>
    internal string Fpi { get; }

    /// <summary>Gets the <c>icon</c> of the diagnostic or property, empty when it has none.</summary>
    internal string Icon { get; }

    /// <summary>Gets the <c>see</c> of the diagnostic or property, empty when it has none.</summary>
    internal string See { get; }

    /// <summary>Gets a value indicating whether a part is an <c>xsl:copy-of</c>, so the property has structured content besides its text.</summary>
    internal bool HasCopyOf { get; }
}
