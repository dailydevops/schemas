namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <see langword="let"/> element.</summary>
public sealed class SchematronLet : SchematronNode
{
    internal SchematronLet(
        DocumentLocation location,
        string name,
        string? declaredType,
        string value,
        IReadOnlyList<SchematronNode>? content = null
    )
        : base(location)
    {
        Name = name;
        DeclaredType = declaredType;
        Value = value;
        Content = content;
    }

    /// <summary>Gets the variable name.</summary>
    public string Name { get; }

    /// <summary>Gets the <see langword="as"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? DeclaredType { get; }

    /// <summary>Gets the raw <see langword="value"/> expression.</summary>
    public string Value { get; }

    /// <summary>
    /// Gets the element content of a <see langword="let"/> without a <see langword="value"/> attribute, as <see cref="SchematronForeignElement"/>
    /// and <see cref="SchematronText"/> nodes in document order, or <see langword="null"/> when the variable has a value expression.
    /// </summary>
    internal IReadOnlyList<SchematronNode>? Content { get; }
}
