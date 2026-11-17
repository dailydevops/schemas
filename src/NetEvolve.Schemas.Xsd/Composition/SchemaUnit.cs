namespace NetEvolve.Schemas.Xsd.Composition;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>One schema document processed under one effective target namespace (design #146 DP-3).</summary>
/// <remarks>A chameleon include is the same document in a second unit with the namespace of its includer.</remarks>
internal sealed class SchemaUnit
{
    internal SchemaUnit(SchemaDocument document, SchemaSyntaxNode root, string effectiveNamespace)
    {
        Argument.ThrowIfNull(document);
        Argument.ThrowIfNull(root);
        Argument.ThrowIfNull(effectiveNamespace);

        Document = document;
        Root = root;
        EffectiveNamespace = effectiveNamespace;
    }

    /// <summary>Gets the parsed document.</summary>
    internal SchemaDocument Document { get; }

    /// <summary>Gets the <c>schema</c> element.</summary>
    internal SchemaSyntaxNode Root { get; }

    /// <summary>Gets the effective target namespace, empty for no namespace.</summary>
    internal string EffectiveNamespace { get; }

    /// <summary>
    /// Gets the top-level components in document order: the declarations and definitions of the document, with the
    /// definitions of a <c>redefine</c> at the position of that element.
    /// </summary>
    internal List<SchemaSyntaxNode> Components { get; } = [];

    /// <summary>Gets the units this unit includes or redefines.</summary>
    internal List<SchemaUnit> Included { get; } = [];
}
