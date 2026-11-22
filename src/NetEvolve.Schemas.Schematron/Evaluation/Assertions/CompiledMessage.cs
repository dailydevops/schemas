namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Arguments;

/// <summary>
/// The rich text of an assertion, a diagnostic, or a property, compiled once in the scope of its rule. The inline elements
/// <c>emph</c>, <c>dir</c>, and <c>span</c> stay in the structure; building the text contributes their content without markup.
/// </summary>
internal sealed class CompiledMessage
{
    internal CompiledMessage(string id, DocumentLocation location, MessagePart[] parts)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(parts);

        Id = id;
        Location = location;
        Parts = parts;
        for (var i = 0; i < parts.Length; i++)
        {
            HasCopyOf |= parts[i].Kind == MessagePartKind.CopyOf;
        }
    }

    /// <summary>Gets the id of the diagnostic or property, empty for the message of the assertion itself.</summary>
    internal string Id { get; }

    internal DocumentLocation Location { get; }

    internal MessagePart[] Parts { get; }

    /// <summary>Gets a value indicating whether a part is an <c>xsl:copy-of</c>, so the property has structured content besides its text.</summary>
    internal bool HasCopyOf { get; }
}
