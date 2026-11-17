namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>A reference to a module to load.</summary>
internal readonly struct ModuleReference
{
    internal ModuleReference(ModuleKind kind, string href, string? baseUri, DocumentLocation location)
    {
        Kind = kind;
        Href = href;
        BaseUri = baseUri;
        Location = location;
    }

    internal ModuleKind Kind { get; }

    internal string Href { get; }

    internal string? BaseUri { get; }

    internal DocumentLocation Location { get; }
}
