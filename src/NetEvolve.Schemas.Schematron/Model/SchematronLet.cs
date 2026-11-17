namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <see langword="let"/> element.</summary>
public sealed class SchematronLet : SchematronNode
{
    internal SchematronLet(DocumentLocation location, string name, string? declaredType, string value)
        : base(location)
    {
        Name = name;
        DeclaredType = declaredType;
        Value = value;
    }

    /// <summary>Gets the variable name.</summary>
    public string Name { get; }

    /// <summary>Gets the <see langword="as"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? DeclaredType { get; }

    /// <summary>Gets the raw <see langword="value"/> expression.</summary>
    public string Value { get; }
}
