namespace NetEvolve.Schemas.Schematron.Model;

// Internal parser intermediate: an xsl:copy-of in the content of a property, under an XSLT query binding (SCH-84, #289). It is a part of
// the rich text of the property, so the structure of the message stays one list, but it is never part of the public model.
internal sealed class SchematronCopyOf : SchematronMessagePart
{
    internal SchematronCopyOf(DocumentLocation location, string select)
        : base(location) => Select = select;

    // The raw select expression.
    internal string Select { get; }
}
