namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>p</c> element.</summary>
public sealed class SchematronParagraph : SchematronRichText
{
    internal SchematronParagraph(
        DocumentLocation location,
        IReadOnlyList<SchematronMessagePart> parts,
        string? id,
        string? styleClass,
        string? icon
    )
        : base(location, parts)
    {
        Id = id;
        StyleClass = styleClass;
        Icon = icon;
    }

    /// <summary>Gets the <c>id</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Id { get; }

    /// <summary>Gets the <see langword="class"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? StyleClass { get; }

    /// <summary>Gets the <c>icon</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Icon { get; }
}
