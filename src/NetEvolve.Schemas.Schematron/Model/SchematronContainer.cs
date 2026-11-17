namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>Base of the elements that hold patterns, parameters, variables, diagnostics, and properties: <c>schema</c> and <c>library</c>.</summary>
public abstract class SchematronContainer : SchematronNode
{
    private protected SchematronContainer(
        DocumentLocation location,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronParameter> parameters,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronPatternBase> patterns,
        SchematronDiagnostics? diagnostics,
        SchematronProperties? properties
    )
        : base(location)
    {
        Title = title;
        Paragraphs = paragraphs;
        Parameters = parameters;
        Lets = lets;
        Patterns = patterns;
        Diagnostics = diagnostics;
        Properties = properties;
    }

    /// <summary>Gets the title, or <see langword="null"/> when absent.</summary>
    public SchematronTitle? Title { get; }

    /// <summary>Gets the paragraphs.</summary>
    public IReadOnlyList<SchematronParagraph> Paragraphs { get; }

    /// <summary>Gets the parameters.</summary>
    public IReadOnlyList<SchematronParameter> Parameters { get; }

    /// <summary>Gets the variables.</summary>
    public IReadOnlyList<SchematronLet> Lets { get; }

    /// <summary>Gets the patterns and groups in document order.</summary>
    public IReadOnlyList<SchematronPatternBase> Patterns { get; }

    /// <summary>Gets the diagnostics, or <see langword="null"/> when absent.</summary>
    public SchematronDiagnostics? Diagnostics { get; }

    /// <summary>Gets the properties, or <see langword="null"/> when absent.</summary>
    public SchematronProperties? Properties { get; }
}
