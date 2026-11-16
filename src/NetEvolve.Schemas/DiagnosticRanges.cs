namespace NetEvolve.Schemas;

/// <summary>The fixed diagnostic code range of every package. Owning packages may append codes, never renumber or reuse.</summary>
public static class DiagnosticRanges
{
    /// <summary>Gets the core range, NES0001 to NES0999.</summary>
    public static DiagnosticCodeRange Core { get; } = new(1, 999);

    /// <summary>Gets the Xml range, NES1000 to NES1999 (well-formedness, DTD validity, datatypes).</summary>
    public static DiagnosticCodeRange Xml { get; } = new(1000, 1999);

    /// <summary>Gets the XPath range, NES2000 to NES2999.</summary>
    public static DiagnosticCodeRange XPath { get; } = new(2000, 2999);

    /// <summary>Gets the Schematron range, NES3000 to NES3999.</summary>
    public static DiagnosticCodeRange Schematron { get; } = new(3000, 3999);

    /// <summary>Gets the Xsd range, NES4000 to NES4999.</summary>
    public static DiagnosticCodeRange Xsd { get; } = new(4000, 4999);

    /// <summary>Gets the dependency injection range, NES5000 to NES5999.</summary>
    public static DiagnosticCodeRange DependencyInjection { get; } = new(5000, 5999);

    /// <summary>Gets the range reserved for future packages, NES6000 to NES8999.</summary>
    public static DiagnosticCodeRange Reserved { get; } = new(6000, 8999);

    /// <summary>Gets the range for consumers, NES9000 to NES9999, never used by the library.</summary>
    public static DiagnosticCodeRange Consumer { get; } = new(9000, 9999);
}
