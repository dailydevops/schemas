namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Tells from the syntax of the schema documents which of the optional listeners a validator needs (design #146 DP-12): the ID
/// listener for a schema that names <c>ID</c>, <c>IDREF</c>, <c>IDREFS</c>, or <c>NOTATION</c> (it checks the notation values as
/// well), the entity listener for a schema that names <c>ENTITY</c> or <c>ENTITIES</c>.
/// </summary>
/// <remarks>
/// A type that derives from one of them names it as its base, a list names it as its item type, and a union as a member type, so
/// the names in the <c>type</c>, <see langword="base"/>, <c>itemType</c>, and <c>memberTypes</c> attributes cover every use. Limitation: the
/// scan is conservative and counts a definition that no declaration uses; an instance whose <c>xsi:type</c> names one of these
/// built-in types while the schema never names it is not checked, because the built-in type is not part of the schema.
/// </remarks>
internal static class ListenerDemand
{
    private static readonly SchemaNamespaceScope NoNamespace = new(string.Empty, false);

    private static readonly string[] TypeAttributes = ["type", "base", "itemType", "memberTypes"];

    /// <summary>Scans one schema document.</summary>
    /// <param name="node">The element to scan, with its descendants.</param>
    /// <returns>The listeners that the document needs.</returns>
    internal static ListenerKinds Scan(SchemaSyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var kinds = ListenerKinds.None;
        for (var i = 0; i < node.AttributeCount; i++)
        {
            var attribute = node.GetAttribute(i);
            if (
                attribute.Name.NamespaceName.Length == 0
                && Array.IndexOf(TypeAttributes, attribute.Name.LocalName) >= 0
            )
            {
                kinds |= KindsOf(node, attribute.Value);
            }
        }

        for (var i = 0; i < node.ChildCount; i++)
        {
            kinds |= Scan(node.GetChildNode(i));
        }

        return kinds;
    }

    private static ListenerKinds KindsOf(SchemaSyntaxNode node, string value)
    {
        var kinds = ListenerKinds.None;
        foreach (var token in SyntaxValues.Split(value))
        {
            if (
                NoNamespace.TryResolve(node, token, out var name)
                && string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            )
            {
                kinds |= name.LocalName switch
                {
                    "ID" or "IDREF" or "IDREFS" or "NOTATION" => ListenerKinds.Ids,
                    "ENTITY" or "ENTITIES" => ListenerKinds.Entities,
                    _ => ListenerKinds.None,
                };
            }
        }

        return kinds;
    }
}
