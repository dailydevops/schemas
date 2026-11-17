namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

// Internal parser intermediate: the reserved <rules> container, rejected as unsupported in release 1.
internal sealed class SchematronRules : SchematronNode
{
    internal SchematronRules(
        DocumentLocation location,
        string? id,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronRule> rules
    )
        : base(location)
    {
        Id = id;
        Title = title;
        Paragraphs = paragraphs;
        Rules = rules;
    }

    internal string? Id { get; }

    internal SchematronTitle? Title { get; }

    internal IReadOnlyList<SchematronParagraph> Paragraphs { get; }

    internal IReadOnlyList<SchematronRule> Rules { get; }
}
