namespace NetEvolve.Schemas.Schematron.Scopes;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The lexical constants of variable values and the <see langword="as"/> types of release 1 (design #150 EP-5, #106 Q2). A value is a
/// constant only when it is one string literal, one numeric literal, or one <c>xs:</c> constructor applied to one literal; nothing
/// else is ever folded, because a compiled schema is shared by every run. The check is Schematron's own: it reads the syntax tree
/// of the XPath parser and does not depend on the XPath package folding anything.
/// </summary>
/// <remarks>
/// A type name is recognized by its namespace, not by its prefix: the prefix is resolved through the <c>ns</c> elements of the
/// schema, the last declaration of a prefix wins as in the static context of the XPath package, and the prefix <c>xs</c> is
/// predeclared for the XML Schema namespace as XPath 2.0 predeclares it. The canonical form handed on is always <c>xs:</c> plus the
/// local name.
/// </remarks>
internal static class ConstantValue
{
    private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
    private const string XmlSchemaPrefix = "xs";

    /// <summary>Determines whether the <see langword="as"/> type is one of the supported types: <c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and <c>xs:integer</c>.</summary>
    /// <param name="written">The <see langword="as"/> attribute as written.</param>
    /// <param name="namespaces">The <c>ns</c> elements of the schema.</param>
    /// <param name="type">The canonical type name, such as <c>xs:integer</c>, when the type is supported; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the type is supported.</returns>
    internal static bool TryGetSupportedType(
        string written,
        IReadOnlyList<SchematronNamespace> namespaces,
        out string? type
    )
    {
        Argument.ThrowIfNull(written);
        Argument.ThrowIfNull(namespaces);

        type = null;
        var colon = written.AsSpan().IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        var local = written.Substring(colon + 1);
        if (
            !IsXmlSchema(written.Substring(0, colon), namespaces)
            || local is not ("string" or "boolean" or "double" or "integer")
        )
        {
            return false;
        }

        type = XmlSchemaPrefix + ":" + local;
        return true;
    }

    /// <summary>Determines the type of a constant.</summary>
    /// <param name="text">The value expression.</param>
    /// <param name="namespaces">The <c>ns</c> elements of the schema.</param>
    /// <param name="type">The canonical type name, such as <c>xs:integer</c>, when the text is a constant; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the text is a constant.</returns>
    internal static bool TryGetType(
        ReadOnlySpan<char> text,
        IReadOnlyList<SchematronNamespace> namespaces,
        out string? type
    )
    {
        Argument.ThrowIfNull(namespaces);

        type = null;
        if (!XPathParser.TryParse(text, out var tree, out _))
        {
            return false;
        }

        type = LiteralType(tree);
        if (
            type is null
            && tree is FunctionCall { Name.Prefix: { } prefix, Arguments.Count: 1 } constructor
            && IsXmlSchema(prefix, namespaces)
            && LiteralType(constructor.Arguments[0]) is not null
        )
        {
            type = XmlSchemaPrefix + ":" + constructor.Name.LocalName;
        }

        return type is not null;
    }

    /// <summary>Determines whether a constant of a type is accepted where the declared type is required, by the function conversion rules (type promotion included).</summary>
    /// <param name="constantType">The canonical type of the constant.</param>
    /// <param name="declaredType">The canonical supported <see langword="as"/> type.</param>
    /// <returns><see langword="true"/> when the value matches.</returns>
    internal static bool Satisfies(string constantType, string declaredType)
    {
        Argument.ThrowIfNull(constantType);
        Argument.ThrowIfNull(declaredType);

        if (string.Equals(constantType, declaredType, StringComparison.Ordinal))
        {
            return true;
        }

        return declaredType switch
        {
            "xs:double" => constantType is "xs:integer" or "xs:decimal" or "xs:float",
            "xs:string" => constantType is "xs:anyURI",
            _ => false,
        };
    }

    private static bool IsXmlSchema(string prefix, IReadOnlyList<SchematronNamespace> namespaces)
    {
        for (var i = namespaces.Count - 1; i >= 0; i--)
        {
            if (string.Equals(namespaces[i].Prefix, prefix, StringComparison.Ordinal))
            {
                return string.Equals(namespaces[i].NamespaceName, XmlSchemaNamespace, StringComparison.Ordinal);
            }
        }

        return string.Equals(prefix, XmlSchemaPrefix, StringComparison.Ordinal);
    }

    private static string? LiteralType(Expression? expression) =>
        expression switch
        {
            StringLiteral => "xs:string",
            IntegerLiteral => "xs:integer",
            DecimalLiteral => "xs:decimal",
            DoubleLiteral => "xs:double",
            _ => null,
        };
}
