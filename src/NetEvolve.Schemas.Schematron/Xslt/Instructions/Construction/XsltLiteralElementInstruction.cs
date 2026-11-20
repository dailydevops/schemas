namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The literal result element (XSLT 2.0 section 11.1), the entry of <see cref="XsltSyntheticElement.LiteralResultElement"/>: an element
/// outside the XSLT namespace in a sequence constructor. The new element has the name of the literal element, a namespace node for
/// each namespace in scope of the literal element except the excluded namespaces, an attribute for each attribute that is not in the
/// XSLT namespace (its value is an attribute value template), and the content of the sequence constructor.
/// </summary>
/// <remarks>
/// Limitations: <c>xsl:use-attribute-sets</c> (#34), <c>xsl:type</c> and <c>xsl:validation</c> (#23),
/// <c>xsl:extension-element-prefixes</c>, <c>xsl:default-collation</c>, and <c>xsl:xpath-default-namespace</c> (#815), and
/// <c>xsl:inherit-namespaces="no"</c> (#813) are the unsupported construct error. The excluded namespaces are the XSLT namespace and
/// those that <c>exclude-result-prefixes</c> designates on the element and its ancestors; an invalid token of an ancestor is not
/// reported here, the module that holds the ancestor owns that check. Namespace aliasing (#35) is not supported.
/// </remarks>
internal sealed class XsltLiteralElementInstruction : IXsltInstruction
{
    private const string ExcludeResultPrefixes = "exclude-result-prefixes";

    /// <inheritdoc />
    public XPathQName Name => default;

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var node = element.Node;
        var names = element.Tree!.Names;
        var name = node.Name;
        var excluded = new List<string>();
        var ok = TryCollectExcluded(context, element, excluded);

        var attributes = new List<LiteralAttribute>();
        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            ok &= TryReadAttribute(context, element, attribute, names, attributes);
        }

        var namespaces = new List<NamespaceBinding>();
        for (var ns = node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            var uri = ns.StringValue;
            if (
                !string.Equals(uri, XsltInstructionRegistry.Namespace, StringComparison.Ordinal)
                && !excluded.Contains(uri)
            )
            {
                namespaces.Add(new NamespaceBinding(names.GetString(ns.Name.Local), uri));
            }
        }

        ok &= FlowCompile.TryCompileBody(context, node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltLiteralElementExecutable(
            new ExpandedName(
                names.GetString(name.Prefix),
                names.GetString(name.Local),
                names.GetString(name.NamespaceUri)
            ),
            namespaces.ToArray(),
            attributes.ToArray(),
            body,
            XsltElementResolver.LocationOf(element, null),
            slotEnd
        );
        return true;
    }

    private static bool TryReadAttribute(
        XsltCompileContext context,
        XPathNode element,
        XmlNode attribute,
        XmlNameTable names,
        List<LiteralAttribute> attributes
    )
    {
        var name = attribute.Name;
        var local = names.GetString(name.Local);
        var prefix = names.GetString(name.Prefix);
        var uri = string.Equals(prefix, "xml", StringComparison.Ordinal)
            ? ContentWriter.XmlNamespace
            : names.GetString(name.NamespaceUri);
        if (string.Equals(uri, XsltInstructionRegistry.Namespace, StringComparison.Ordinal))
        {
            return TryReadStandard(context, element, local, attribute.StringValue);
        }

        if (!AttributeValueTemplate.TryCompile(context, element, local, attribute.StringValue, out var template))
        {
            return false;
        }

        attributes.Add(new LiteralAttribute(prefix, local, uri, template));
        return true;
    }

    // An attribute in the XSLT namespace is one of the standard attributes; none of them becomes an attribute of the result.
    private static bool TryReadStandard(XsltCompileContext context, XPathNode element, string local, string value)
    {
        var written = "xsl:" + local;
        switch (local)
        {
            case "version":
            case ExcludeResultPrefixes:
                return true;
            case "inherit-namespaces":
                return ConstructionCompile.TryAcceptSwitch(context, element, written, "literal result", value);
            case "use-attribute-sets":
                ConstructionCompile.ReportUnsupported(
                    context,
                    element,
                    written,
                    "literal result",
                    ConstructionCompile.AttributeSetReason
                );
                return false;
            case "type":
            case "validation":
                ConstructionCompile.ReportUnsupported(
                    context,
                    element,
                    written,
                    "literal result",
                    ConstructionCompile.SchemaReason
                );
                return false;
            case "extension-element-prefixes":
            case "default-collation":
            case "xpath-default-namespace":
                ConstructionCompile.ReportUnsupported(
                    context,
                    element,
                    written,
                    "literal result",
                    ConstructionCompile.StandardReason
                );
                return false;
            default:
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.AttributeInvalid,
                    $"The attribute '{written}' is not defined for a literal result element.",
                    element,
                    "XTSE0805"
                );
                return false;
        }
    }

    // The namespaces that exclude-result-prefixes designates on the element and on its ancestors (XSLT 2.0 section 11.1.3).
    private static bool TryCollectExcluded(XsltCompileContext context, XPathNode element, List<string> excluded)
    {
        var ok = true;
        var names = element.Tree!.Names;
        for (
            var current = element.Node;
            !current.IsEmpty && current.Kind == XmlNodeKind.Element;
            current = current.Parent
        )
        {
            var value = ReadExclude(current, names);
            if (value is not null)
            {
                ok &= Exclude(context, element, current, value, current == element.Node, excluded);
            }
        }

        return ok;
    }

    // The attribute is in the XSLT namespace unless its parent is an XSLT element.
    private static string? ReadExclude(XmlNode node, XmlNameTable names)
    {
        var inXslt = string.Equals(
            names.GetString(node.Name.NamespaceUri),
            XsltInstructionRegistry.Namespace,
            StringComparison.Ordinal
        );
        for (var attribute = node.FirstAttribute; !attribute.IsEmpty; attribute = attribute.NextAttribute)
        {
            var name = attribute.Name;
            if (
                string.Equals(names.GetString(name.Local), ExcludeResultPrefixes, StringComparison.Ordinal)
                && string.Equals(
                    names.GetString(name.NamespaceUri),
                    inXslt ? string.Empty : XsltInstructionRegistry.Namespace,
                    StringComparison.Ordinal
                )
            )
            {
                return attribute.StringValue;
            }
        }

        return null;
    }

    private static bool Exclude(
        XsltCompileContext context,
        XPathNode element,
        XmlNode owner,
        string value,
        bool report,
        List<string> excluded
    )
    {
        var ok = true;
        var ownerNode = new XPathNode(owner);
        foreach (var token in value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(token, "#all", StringComparison.Ordinal))
            {
                for (var ns = owner.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
                {
                    excluded.Add(ns.StringValue);
                }

                continue;
            }

            var isDefault = string.Equals(token, "#default", StringComparison.Ordinal);
            if (XsltElementReader.TryResolvePrefix(ownerNode, isDefault ? string.Empty : token, out var uri))
            {
                excluded.Add(uri);
            }
            else if (report)
            {
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.AttributeInvalid,
                    isDefault
                        ? "The value #default of exclude-result-prefixes needs a default namespace."
                        : $"The prefix '{token}' of exclude-result-prefixes is not bound to a namespace.",
                    element,
                    isDefault ? "XTSE0809" : "XTSE0808"
                );
                ok = false;
            }
        }

        return ok;
    }
}
