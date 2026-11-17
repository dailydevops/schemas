namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>active</c> element of a phase.</summary>
public sealed class SchematronActive : SchematronRichText
{
    internal SchematronActive(DocumentLocation location, IReadOnlyList<SchematronMessagePart> parts, string pattern)
        : base(location, parts) => Pattern = pattern;

    /// <summary>Gets the <c>pattern</c> attribute: the id of the activated pattern or group.</summary>
    public string Pattern { get; }
}
