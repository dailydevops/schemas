namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>ns</c> element: a prefix bound to a namespace for use in expressions.</summary>
public sealed class SchematronNamespace : SchematronNode
{
    internal SchematronNamespace(DocumentLocation location, string prefix, string namespaceName)
        : base(location)
    {
        Prefix = prefix;
        NamespaceName = namespaceName;
    }

    /// <summary>Gets the prefix.</summary>
    public string Prefix { get; }

    /// <summary>Gets the namespace name.</summary>
    public string NamespaceName { get; }
}
