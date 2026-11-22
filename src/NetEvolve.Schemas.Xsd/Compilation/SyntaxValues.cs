namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Reads the attribute values and child shapes of schema syntax that every component builder needs.</summary>
internal static class SyntaxValues
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    /// <summary>Tells whether a node is a schema element with a local name.</summary>
    /// <param name="node">The node.</param>
    /// <param name="localName">The local name.</param>
    /// <returns><see langword="true"/> for an element of the schema namespace with that name.</returns>
    internal static bool IsSchemaElement(SchemaSyntaxNode node, string localName)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(localName);

        return string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && string.Equals(node.Name.LocalName, localName, StringComparison.Ordinal);
    }

    /// <summary>Finds the first child that is a schema element with a local name.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="localName">The local name.</param>
    /// <returns>The child, <see langword="null"/> when there is none.</returns>
    internal static SchemaSyntaxNode? FindChild(SchemaSyntaxNode parent, string localName)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(localName);

        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildNode(i);
            if (IsSchemaElement(child, localName))
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>Gets the root element of the document that holds a node.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The <c>schema</c> element.</returns>
    internal static SchemaSyntaxNode Root(SchemaSyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        while (node.Parent is not null)
        {
            node = node.Parent;
        }

        return node;
    }

    /// <summary>Reads a boolean attribute value.</summary>
    /// <param name="value">The value, or <see langword="null"/> when the attribute is absent.</param>
    /// <returns><see langword="true"/> for the values true and 1.</returns>
    internal static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.Ordinal) || string.Equals(value, "1", StringComparison.Ordinal);

    /// <summary>Splits a whitespace separated list value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The items, none of them empty.</returns>
    internal static string[] Split(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Reads a derivation set: <c>#all</c> or a list of method names; names outside the mask do not apply.</summary>
    /// <param name="value">The value, or <see langword="null"/> when the attribute is absent.</param>
    /// <param name="mask">The methods the attribute may name.</param>
    /// <returns>The methods.</returns>
    internal static XsdDerivationMethod ParseMethods(string? value, XsdDerivationMethod mask)
    {
        if (value is null)
        {
            return XsdDerivationMethod.None;
        }

        var methods = XsdDerivationMethod.None;
        foreach (var token in Split(value))
        {
            methods |= token switch
            {
                "#all" => mask,
                "extension" => XsdDerivationMethod.Extension,
                "restriction" => XsdDerivationMethod.Restriction,
                "list" => XsdDerivationMethod.List,
                "union" => XsdDerivationMethod.Union,
                "substitution" => XsdDerivationMethod.Substitution,
                _ => XsdDerivationMethod.None,
            };
        }

        return methods & mask;
    }
}
