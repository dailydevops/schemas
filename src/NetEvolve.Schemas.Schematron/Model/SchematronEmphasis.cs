namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>emph</c> element. Its text is contributed to messages without markup.</summary>
public sealed class SchematronEmphasis : SchematronMessagePart
{
    internal SchematronEmphasis(DocumentLocation location, IReadOnlyList<SchematronMessagePart> parts)
        : base(location) => Parts = parts;

    /// <summary>Gets the inner parts.</summary>
    public IReadOnlyList<SchematronMessagePart> Parts { get; }
}
