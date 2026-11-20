namespace NetEvolve.Schemas.Schematron.Scopes;

using NetEvolve.Arguments;

/// <summary>
/// A top-level <c>xsl:variable</c> or <c>xsl:param</c> reduced to what the planner needs (design #150 EP-4): the name as written, the
/// text of the value, the canonical <see langword="as"/> type, and whether the declaration is a parameter or a required one. The
/// planner gives it a slot of the schema level and compiles the value as it compiles the value of a <see langword="let"/>, so the
/// scopes know nothing of XSLT.
/// </summary>
internal sealed class GlobalDeclaration
{
    internal GlobalDeclaration(
        string name,
        string? value,
        string? declaredType,
        bool isParameter,
        bool isRequired,
        DocumentLocation location
    )
    {
        Argument.ThrowIfNull(name);

        Name = name;
        Value = value;
        DeclaredType = declaredType;
        IsParameter = isParameter;
        IsRequired = isRequired;
        Location = location;
    }

    /// <summary>Gets the name as written; it is the key of the caller value of a parameter.</summary>
    internal string Name { get; }

    /// <summary>Gets the value expression, or <see langword="null"/> for a required parameter, which has no default.</summary>
    internal string? Value { get; }

    /// <summary>Gets the canonical <see langword="as"/> type, such as <c>xs:integer</c>, or <see langword="null"/>.</summary>
    internal string? DeclaredType { get; }

    /// <summary>Gets a value indicating whether the declaration is an <c>xsl:param</c>, which the caller can set.</summary>
    internal bool IsParameter { get; }

    /// <summary>Gets a value indicating whether the caller must supply a value.</summary>
    internal bool IsRequired { get; }

    /// <summary>Gets the location of the element.</summary>
    internal DocumentLocation Location { get; }
}
