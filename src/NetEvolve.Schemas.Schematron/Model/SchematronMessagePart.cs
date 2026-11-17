namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>Base of the parts of rich text: character data and the inline elements <c>name</c>, <c>value-of</c>, <c>emph</c>, <c>dir</c>, and <c>span</c>.</summary>
public abstract class SchematronMessagePart : SchematronNode
{
    private protected SchematronMessagePart(DocumentLocation location)
        : base(location) { }
}
