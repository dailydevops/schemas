namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.V30;

using System;
using System.Reflection;
using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Parses text and renders the tree as <c>Type&lt;scalar=value,...&gt;(child child)</c>; a QName renders as its name.
/// </summary>
internal static class XPath30Dump
{
    internal static string Tree(string text, XPathVersion version = XPathVersion.V30)
    {
        if (!XPathParser.TryParse(text.AsSpan(), version, out var expression, out var diagnostic))
        {
            return $"ERROR {diagnostic.Code} at {diagnostic.Offset}";
        }

        var builder = new StringBuilder();
        Write(builder, expression!);
        return builder.ToString();
    }

    internal static XPathDiagnostic Diagnose(string text, XPathVersion version)
    {
        _ = XPathParser.TryParse(text.AsSpan(), version, out _, out var diagnostic);
        return diagnostic;
    }

    private static void Write(StringBuilder builder, SyntaxNode node)
    {
        if (node is QualifiedName name)
        {
            var uri = Scalar(name, "NamespaceUri");
            _ =
                uri is not null ? builder.Append("Q{").Append(uri).Append('}').Append(name.LocalName)
                : name.Prefix is null ? builder.Append(name.LocalName)
                : builder.Append(name.Prefix).Append(':').Append(name.LocalName);
            return;
        }

        _ = builder.Append(node.GetType().Name);
        var scalars = new StringBuilder();
        foreach (var property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (
                property.Name is nameof(SyntaxNode.Location) or nameof(SyntaxNode.Children)
                || typeof(SyntaxNode).IsAssignableFrom(property.PropertyType)
                || property.PropertyType.IsGenericType
            )
            {
                continue;
            }

            var value = property.GetValue(node);
            if (value is not null)
            {
                _ = scalars.Append(scalars.Length == 0 ? "" : ",").Append(property.Name).Append('=').Append(value);
            }
        }

        if (scalars.Length > 0)
        {
            _ = builder.Append('<').Append(scalars).Append('>');
        }

        if (node.Children.Count == 0)
        {
            return;
        }

        _ = builder.Append('(');
        for (var i = 0; i < node.Children.Count; i++)
        {
            _ = i == 0 ? builder : builder.Append(' ');
            Write(builder, node.Children[i]);
        }

        _ = builder.Append(')');
    }

    private static string? Scalar(object node, string property) =>
        node.GetType().GetProperty(property)?.GetValue(node) as string;
}
