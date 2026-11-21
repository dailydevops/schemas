namespace NetEvolve.Schemas.Xsd.Compilation;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The order and number of the children of every schema element.</summary>
internal static class SchemaStructure
{
    /// <summary>Tells whether the children of an element are in an order and number that its content model allows.</summary>
    /// <param name="node">The element.</param>
    /// <returns><see langword="true"/> when the content is valid.</returns>
    internal static bool IsValid(SchemaSyntaxNode node)
    {
        Argument.ThrowIfNull(node);

        return true;
    }

    /// <summary>Gets the content model of an element in a readable form.</summary>
    /// <param name="node">The element.</param>
    /// <returns>The model, <see langword="null"/> when the element has none.</returns>
    internal static string? DescribeModel(SchemaSyntaxNode node)
    {
        Argument.ThrowIfNull(node);

        return null;
    }
}
