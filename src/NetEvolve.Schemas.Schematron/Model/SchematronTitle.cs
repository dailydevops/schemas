namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>title</c> element.</summary>
public sealed class SchematronTitle : SchematronRichText
{
    internal SchematronTitle(DocumentLocation location, IReadOnlyList<SchematronMessagePart> parts)
        : base(location, parts) { }
}
