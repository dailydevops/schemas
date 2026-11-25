namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>diagnostic</c> element.</summary>
public sealed class SchematronDiagnostic : SchematronNode
{
    internal SchematronDiagnostic(
        DocumentLocation location,
        string id,
        string? role,
        string? fpi,
        string? icon,
        string? see,
        SchematronMessage message
    )
        : base(location)
    {
        Id = id;
        Role = role;
        Fpi = fpi;
        Icon = icon;
        See = see;
        Message = message;
    }

    /// <summary>Gets the id.</summary>
    public string Id { get; }

    /// <summary>Gets the <c>role</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Role { get; }

    /// <summary>Gets the <c>fpi</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Fpi { get; }

    /// <summary>Gets the <c>icon</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Icon { get; }

    /// <summary>Gets the <c>see</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? See { get; }

    /// <summary>Gets the message.</summary>
    public SchematronMessage Message { get; }
}
