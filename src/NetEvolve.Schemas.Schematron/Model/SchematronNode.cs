namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>Base of every element of the Schematron model: immutable, with the location of its source element.</summary>
public abstract class SchematronNode
{
    private protected SchematronNode(DocumentLocation location) => Location = location;

    /// <summary>Gets the location of the element: the module it came from and its start position.</summary>
    public DocumentLocation Location { get; }
}
