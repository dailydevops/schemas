namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>property</c> element.</summary>
public sealed class SchematronProperty : SchematronNode
{
    internal SchematronProperty(
        DocumentLocation location,
        string id,
        string? role,
        string? scheme,
        SchematronMessage message
    )
        : base(location)
    {
        Id = id;
        Role = role;
        Scheme = scheme;
        Message = message;
    }

    /// <summary>Gets the id.</summary>
    public string Id { get; }

    /// <summary>Gets the <c>role</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Role { get; }

    /// <summary>Gets the <c>scheme</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Scheme { get; }

    /// <summary>Gets the message.</summary>
    public SchematronMessage Message { get; }
}
