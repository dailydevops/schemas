namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The rich text of an assertion, diagnostic, or property.</summary>
public sealed class SchematronMessage : SchematronRichText
{
    internal SchematronMessage(
        DocumentLocation location,
        IReadOnlyList<SchematronMessagePart> parts,
        string? language = null
    )
        : base(location, parts) => Language = language;

    /// <summary>Gets the language in effect: the nearest <c>xml:lang</c> of the element and its ancestors; <see langword="null"/> or empty when none.</summary>
    internal string? Language { get; }
}
