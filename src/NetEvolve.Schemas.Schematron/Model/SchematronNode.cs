namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>Base of every element of the Schematron model: immutable, with the location of its source element.</summary>
public abstract class SchematronNode
{
    private protected SchematronNode(DocumentLocation location) => Location = location;

    /// <summary>Gets the location of the element: the module it came from and its start position.</summary>
    public DocumentLocation Location { get; }

    /// <summary>
    /// Gets the base URI that an <c>xml:base</c> on the element or on an ancestor sets, resolved against the system identifier of the
    /// module, or <see langword="null"/> when none does: expressions then use the system identifier of the schema.
    /// </summary>
    internal string? BaseUri { get; set; }
}
