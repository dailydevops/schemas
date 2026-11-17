namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>name</c> element: the name of a node, selected by an optional path expression.</summary>
public sealed class SchematronName : SchematronMessagePart
{
    internal SchematronName(DocumentLocation location, string? path)
        : base(location) => Path = path;

    /// <summary>Gets the raw <c>path</c> expression, or <see langword="null"/> for the context node.</summary>
    public string? Path { get; }
}
