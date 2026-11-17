namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The rich text of an assertion, diagnostic, or property.</summary>
public sealed class SchematronMessage : SchematronRichText
{
    internal SchematronMessage(DocumentLocation location, IReadOnlyList<SchematronMessagePart> parts)
        : base(location, parts) { }
}
