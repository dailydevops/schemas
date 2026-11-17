namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>Base of the elements that hold rich text: <c>title</c>, <c>p</c>, <c>active</c>, and message elements.</summary>
public abstract class SchematronRichText : SchematronNode
{
    private protected SchematronRichText(DocumentLocation location, IReadOnlyList<SchematronMessagePart> parts)
        : base(location) => Parts = parts;

    /// <summary>Gets the text and inline parts in document order.</summary>
    public IReadOnlyList<SchematronMessagePart> Parts { get; }
}
