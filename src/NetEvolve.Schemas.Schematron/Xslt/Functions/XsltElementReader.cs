namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Reads the attributes, the names, and the in-scope namespaces of the XSLT elements of this folder from the Xml contract node model.</summary>
internal static class XsltElementReader
{
    private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    /// <summary>Reads an attribute without a namespace.</summary>
    /// <param name="element">The element.</param>
    /// <param name="name">The local name of the attribute.</param>
    /// <param name="value">The value when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the attribute is present.</returns>
    internal static bool TryGetAttribute(XPathNode element, string name, out string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        for (var attribute = element.Node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            var qualified = attribute.Name;
            if (
                qualified.NamespaceUri.IsEmpty
                && string.Equals(names.GetString(qualified.Local), name, StringComparison.Ordinal)
            )
            {
                value = attribute.StringValue;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>Reads the required <c>name</c> attribute of a variable or parameter, a QName kept as written; a problem is reported.</summary>
    /// <param name="problems">The sink that receives the problem.</param>
    /// <param name="element">The element.</param>
    /// <param name="elementName">The element as written in a message, such as <c>xsl:variable</c>.</param>
    /// <param name="name">The name when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the element has a valid name.</returns>
    internal static bool TryReadVariableName(
        IProblemSink problems,
        XPathNode element,
        string elementName,
        out string name
    )
    {
        ArgumentNullException.ThrowIfNull(problems);
        ArgumentNullException.ThrowIfNull(elementName);

        if (!TryGetAttribute(element, "name", out name))
        {
            XsltProblems.Report(
                problems,
                SchematronDiagnosticCodes.AttributeMissing,
                $"The {elementName} element has no name attribute.",
                element
            );
            return false;
        }

        if (TryParseQName(name, out _, out _))
        {
            return true;
        }

        XsltProblems.Report(
            problems,
            SchematronDiagnosticCodes.AttributeInvalid,
            $"The name '{name}' of the {elementName} element is not a QName.",
            element,
            "XTSE0020"
        );
        return false;
    }

    /// <summary>Reads the <c>name</c> attribute as written, or an empty name; a failed declaration still takes its slot under this name, so a later reference is not reported again as undefined.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The name, or an empty string.</returns>
    internal static string NameOrEmpty(XPathNode element) =>
        TryGetAttribute(element, "name", out var name) ? name : string.Empty;

    /// <summary>Determines whether a node is an element of the XSLT namespace with a local name.</summary>
    /// <param name="node">The node.</param>
    /// <param name="local">The local name.</param>
    /// <returns><see langword="true"/> for that element.</returns>
    internal static bool IsXslt(XmlNode node, string local)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(node.Tree);

        if (node.Kind != XmlNodeKind.Element)
        {
            return false;
        }

        var names = node.Tree.Names;
        var name = node.Name;
        return !name.NamespaceUri.IsEmpty
            && string.Equals(
                names.GetString(name.NamespaceUri),
                XsltInstructionRegistry.Namespace,
                StringComparison.Ordinal
            )
            && string.Equals(names.GetString(name.Local), local, StringComparison.Ordinal);
    }

    /// <summary>Determines whether a text contains only the characters XML treats as white space.</summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when the text is empty or white space.</returns>
    internal static bool IsWhitespace(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (var c in text)
        {
            if (c is not (' ' or '\t' or '\n' or '\r'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Splits a lexical QName; the XPath lexer decides what a QName is, so the rules are not written twice.</summary>
    /// <param name="text">The text.</param>
    /// <param name="prefix">The prefix, or <see langword="null"/>.</param>
    /// <param name="local">The local name.</param>
    /// <returns><see langword="true"/> when the whole text is one QName.</returns>
    internal static bool TryParseQName(string text, out string? prefix, out string local)
    {
        ArgumentNullException.ThrowIfNull(text);

        prefix = null;
        local = string.Empty;
        if (
            !XPathParser.TryParse(("$" + text).AsSpan(), out var tree, out _)
            || tree is not VariableReference { Name: { } name }
        )
        {
            return false;
        }

        var rebuilt = name.Prefix is null ? name.LocalName : name.Prefix + ":" + name.LocalName;
        if (!string.Equals(rebuilt, text, StringComparison.Ordinal))
        {
            return false;
        }

        prefix = name.Prefix;
        local = name.LocalName;
        return true;
    }

    /// <summary>Looks a prefix up in the in-scope namespaces of an element.</summary>
    /// <param name="element">The element.</param>
    /// <param name="prefix">The prefix; empty for the default namespace.</param>
    /// <param name="namespaceName">The namespace name when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the prefix is bound.</returns>
    internal static bool TryResolvePrefix(XPathNode element, string prefix, out string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        for (var node = element.Node.FirstNamespace; !node.IsEmpty; node = node.NextNamespace)
        {
            var name = node.Name;
            var bound = name.Local.IsEmpty ? string.Empty : names.GetString(name.Local);
            if (string.Equals(bound, prefix, StringComparison.Ordinal))
            {
                namespaceName = node.StringValue;
                return true;
            }
        }

        namespaceName = string.Empty;
        return false;
    }

    /// <summary>
    /// Builds the static context that binds the prefixes of a sequence type: the in-scope namespaces of the element, and <c>xs</c>
    /// for the XML Schema namespace when the element does not bind it (the scopes of the package predeclare it the same way).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The static context.</returns>
    internal static XPathStaticContext StaticContext(XPathNode element)
    {
        ArgumentNullException.ThrowIfNull(element.Tree);

        var names = element.Tree.Names;
        var builder = XPathStaticContext.Default.ToBuilder().WithNamespace("xs", XmlSchemaNamespace);
        for (var node = element.Node.FirstNamespace; !node.IsEmpty; node = node.NextNamespace)
        {
            var name = node.Name;
            if (!name.Local.IsEmpty)
            {
                _ = builder.WithNamespace(names.GetString(name.Local), node.StringValue);
            }
        }

        return builder.Build();
    }
}
