namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The <c>value-of</c> element: the string value of an expression.</summary>
public sealed class SchematronValueOf : SchematronMessagePart
{
    internal SchematronValueOf(DocumentLocation location, string select)
        : base(location) => Select = select;

    /// <summary>Gets the raw <see langword="select"/> expression.</summary>
    public string Select { get; }
}
