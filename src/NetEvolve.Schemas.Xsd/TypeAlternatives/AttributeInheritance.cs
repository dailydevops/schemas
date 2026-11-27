namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The <c>inheritable</c> attribute of an attribute declaration (Structures 1.1, 3.2.2).</summary>
/// <remarks>
/// The attribute declaration builder copies the value to <c>AttributeDeclaration.IsInheritable</c>. The grammar restricts the value to a boolean, and the attribute is false when absent.
/// </remarks>
internal static class AttributeInheritance
{
    /// <summary>Reads the <c>inheritable</c> attribute of an <c>attribute</c> element.</summary>
    /// <param name="attribute">The <c>attribute</c> element.</param>
    /// <returns><see langword="true"/> when the attribute is inheritable.</returns>
    internal static bool IsInheritable(SchemaSyntaxNode attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var value = attribute.FindAttribute("inheritable")?.Trim(' ', '\t', '\r', '\n');
        return string.Equals(value, "true", StringComparison.Ordinal)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }
}
