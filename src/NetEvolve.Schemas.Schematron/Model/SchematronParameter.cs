namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>param</c> element.</summary>
public sealed class SchematronParameter : SchematronNode
{
    internal SchematronParameter(DocumentLocation location, string name, string? value)
        : base(location)
    {
        Name = name;
        Value = value;
    }

    /// <summary>Gets the parameter name.</summary>
    public string Name { get; }

    /// <summary>Gets the raw default value expression, or <see langword="null"/> when absent.</summary>
    public string? Value { get; }
}
