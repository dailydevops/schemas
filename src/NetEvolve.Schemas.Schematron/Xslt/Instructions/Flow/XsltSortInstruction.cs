namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:sort</c> (XSLT 2.0 section 13.1): one sort key component. It is a structural child, registered for
/// <see cref="XsltPosition.Child"/>, so it is misplaced everywhere else; the instruction that sorts reads its keys with
/// <see cref="TryReadKeys"/>. The registry holds one entry per name, so every instruction that takes a sort key specification
/// uses this entry and <see cref="XsltSorter"/>, and none registers <c>xsl:sort</c> again.
/// </summary>
/// <remarks>
/// Limitations: the codepoint collation is the only collation (DEF-43, #55), so <c>lang</c> and <c>case-order</c> are
/// unsupported unless a <c>collation</c> is given, which makes XSLT 2.0 ignore them; <c>data-type</c> is <c>text</c> or absent,
/// <c>number</c> is #682; a sort key written as a sequence constructor is #682; the attributes are constants, not attribute value
/// templates (#681). The sort is always stable, which <c>stable="no"</c> permits.
/// </remarks>
internal sealed class XsltSortInstruction : IXsltInstruction
{
    private const string CodepointCollation = "http://www.w3.org/2005/xpath-functions/collation/codepoint";
    private const string SortLocalName = "sort";
    private static readonly string[] Orders = ["ascending", "descending"];
    private static readonly string[] YesNo = ["yes", "no"];

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, SortLocalName);

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Child;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        var ok = TryRead(context, context.Element, true, out var key);
        executable = ok ? key : null!;
        return ok;
    }

    /// <summary>Reads the <c>xsl:sort</c> elements at the start of the content of an element, skipping white space between them.</summary>
    /// <param name="context">The compile context of the element; its scope is the scope the sort keys are compiled in.</param>
    /// <param name="keys">The keys in order.</param>
    /// <param name="first">The first node after the sort keys; an empty node for none.</param>
    /// <returns><see langword="true"/> when every key compiled.</returns>
    internal static bool TryReadKeys(XsltCompileContext context, List<XsltSortKey> keys, out XmlNode first)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(keys);

        var ok = true;
        var child = context.Element.Node.FirstChild;
        for (; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Text && XsltElementReader.IsWhitespace(child.StringValue))
            {
                continue;
            }

            if (!XsltElementReader.IsXslt(child, SortLocalName))
            {
                break;
            }

            if (TryRead(context, new XPathNode(child), keys.Count == 0, out var key))
            {
                keys.Add(key);
            }
            else
            {
                ok = false;
            }
        }

        first = child;
        return ok;
    }

    private static bool TryRead(XsltCompileContext context, XPathNode element, bool isFirst, out XsltSortKey key)
    {
        key = null!;
        var ok = FlowCompile.TryReadChoice(context, element, "order", "xsl:sort", Orders, out var order);
        ok &= FlowCompile.TryReadChoice(context, element, "stable", "xsl:sort", YesNo, out var stable);
        ok &= FlowCompile.TryReadConstant(context, element, "data-type", "xsl:sort", out var dataType);
        ok &= FlowCompile.TryReadConstant(context, element, "collation", "xsl:sort", out var collation);
        ok &= FlowCompile.TryReadConstant(context, element, "lang", "xsl:sort", out var lang);
        ok &= FlowCompile.TryReadConstant(context, element, "case-order", "xsl:sort", out var caseOrder);

        if (stable is not null && !isFirst)
        {
            FlowCompile.Report(
                context,
                SchematronDiagnosticCodes.AttributeInvalid,
                "Only the first xsl:sort element of a sort key specification has a stable attribute.",
                element,
                "XTSE1017"
            );
            ok = false;
        }

        ok &= CheckSupported(context, element, dataType, collation, lang, caseOrder);
        ok &= TryCompileSelect(context, element, out var select);
        if (!ok)
        {
            return false;
        }

        key = new XsltSortKey(
            select,
            string.Equals(order, "descending", StringComparison.Ordinal),
            string.Equals(dataType, "text", StringComparison.Ordinal),
            XsltElementResolver.LocationOf(element, null)
        );
        return true;
    }

    private static bool CheckSupported(
        XsltCompileContext context,
        XPathNode element,
        string? dataType,
        string? collation,
        string? lang,
        string? caseOrder
    )
    {
        string? reason = null;
        if (dataType is not null && !string.Equals(dataType, "text", StringComparison.Ordinal))
        {
            reason = $"The data-type '{dataType}' is not supported; only text is (#682).";
        }
        else if (collation is not null && !string.Equals(collation, CodepointCollation, StringComparison.Ordinal))
        {
            reason = $"The collation '{collation}' is not supported; only the codepoint collation is (DEF-43, #55).";
        }
        else if (collation is null && (lang is not null || caseOrder is not null))
        {
            reason =
                "The lang and case-order attributes need a collation other than the codepoint collation, which is not supported (DEF-43, #55).";
        }

        if (reason is null)
        {
            return true;
        }

        FlowCompile.Report(context, SchematronDiagnosticCodes.XsltUnsupported, reason, element, null);
        return false;
    }

    // The key is the select attribute, or the item itself when there is none; content in place of select is not supported yet.
    private static bool TryCompileSelect(XsltCompileContext context, XPathNode element, out QueryExpression select)
    {
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var text);
        if (XsltVariableInstruction.HasContent(element))
        {
            FlowCompile.Report(
                context,
                hasSelect ? SchematronDiagnosticCodes.ElementMisplaced : SchematronDiagnosticCodes.XsltUnsupported,
                hasSelect
                    ? "An xsl:sort element has either a select attribute or content, not both."
                    : "The XSLT element 'xsl:sort' with a sequence constructor is not supported; use the select attribute (#682).",
                element,
                hasSelect ? "XTSE1015" : null
            );
            select = null!;
            return false;
        }

        return FlowCompile.TryCompileExpression(context, hasSelect ? text : ".", QueryExpressionKind.Value, out select);
    }
}
