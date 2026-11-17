namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>span</c> element. Its text is contributed to messages without markup.</summary>
public sealed class SchematronSpan : SchematronMessagePart
{
    internal SchematronSpan(DocumentLocation location, string? styleClass, IReadOnlyList<SchematronMessagePart> parts)
        : base(location)
    {
        StyleClass = styleClass;
        Parts = parts;
    }

    /// <summary>Gets the <see langword="class"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? StyleClass { get; }

    /// <summary>Gets the inner parts.</summary>
    public IReadOnlyList<SchematronMessagePart> Parts { get; }
}
