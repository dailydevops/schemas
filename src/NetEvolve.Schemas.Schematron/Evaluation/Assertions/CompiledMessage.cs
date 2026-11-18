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
        Argument.ThrowIfNull(id);
        Argument.ThrowIfNull(parts);

        Id = id;
        Location = location;
        Parts = parts;
    }

    /// <summary>Gets the id of the diagnostic or property, empty for the message of the assertion itself.</summary>
    internal string Id { get; }

    internal DocumentLocation Location { get; }

    internal MessagePart[] Parts { get; }
}
