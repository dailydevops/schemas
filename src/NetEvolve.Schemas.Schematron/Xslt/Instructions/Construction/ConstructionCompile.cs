namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The compile steps that the construction instructions share: a required or optional attribute value template, the content or the
/// select of an instruction that builds a simple node, the attributes that are the unsupported construct error, and the yes or no
/// switches of the namespace handling. Every problem is reported through the context; the compile continues after one.
/// </summary>
internal static class ConstructionCompile
{
    internal const string SchemaReason = "schema-aware construction needs xsl:import-schema (#23)";
    internal const string AttributeSetReason = "attribute sets are not supported (#34)";
    internal const string NamespaceReason = "the control of namespace inheritance is not supported (#813)";
    internal const string StandardReason =
        "this standard attribute is not supported on a literal result element (#815)";

    /// <summary>Reads a required attribute as a template.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The attribute.</param>
    /// <param name="elementName">The element as written in a message, such as <c>xsl:element</c>.</param>
    /// <param name="template">The template when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadRequired(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string elementName,
        out AttributeValueTemplate template
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(elementName);

        if (!XsltElementReader.TryGetAttribute(element, attributeName, out var text))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                $"The {elementName} element has no {attributeName} attribute.",
                element
            );
            template = null!;
            return false;
        }

        return AttributeValueTemplate.TryCompile(context, element, attributeName, text, out template);
    }

    /// <summary>Reads an optional attribute as a template.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The attribute.</param>
    /// <param name="template">The template, or <see langword="null"/> when the attribute is absent.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadOptional(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        out AttributeValueTemplate? template
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attributeName);

        template = null;
        if (!XsltElementReader.TryGetAttribute(element, attributeName, out var text))
        {
            return true;
        }

        var ok = AttributeValueTemplate.TryCompile(context, element, attributeName, text, out var compiled);
        template = ok ? compiled : null;
        return ok;
    }

    /// <summary>
    /// Reads the value of an instruction that builds a simple node: the <see langword="select"/> expression or the content, not both.
    /// An empty content is the empty string.
    /// </summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <param name="constraint">The code of the static error for select and content together.</param>
    /// <param name="select">The expression, or <see langword="null"/> for the content form.</param>
    /// <param name="body">The content, or <see langword="null"/> for the select form.</param>
    /// <param name="slotEnd">The first slot above every slot of the content.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadValue(
        XsltCompileContext context,
        XPathNode element,
        string elementName,
        string constraint,
        out QueryExpression? select,
        out XsltBodyExecutable? body,
        out int slotEnd
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(constraint);

        select = null;
        body = null;
        slotEnd = context.Scope.VariableNames.Count;
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var selectText);
        if (hasSelect && XsltVariableInstruction.HasContent(element))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.ElementMisplaced,
                $"The {elementName} element has either a select attribute or content, not both.",
                element,
                constraint
            );
            return false;
        }

        if (hasSelect)
        {
            return FlowCompile.TryCompileExpression(context, selectText, QueryExpressionKind.Value, out select);
        }

        var ok = FlowCompile.TryCompileBody(context, element.Node.FirstChild, out var compiled, out slotEnd);
        body = ok ? compiled : null;
        return ok;
    }

    /// <summary>Reports the attributes of an element that are the unsupported construct error.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <param name="attributes">The attributes to look for: <c>type</c>, <c>validation</c>, or <c>use-attribute-sets</c>.</param>
    /// <returns><see langword="false"/> when one of them is present.</returns>
    internal static bool CheckUnsupported(
        XsltCompileContext context,
        XPathNode element,
        string elementName,
        params string[] attributes
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(attributes);

        var ok = true;
        foreach (var attribute in attributes)
        {
            if (XsltElementReader.TryGetAttribute(element, attribute, out _))
            {
                ReportUnsupported(
                    context,
                    element,
                    attribute,
                    elementName,
                    string.Equals(attribute, "use-attribute-sets", StringComparison.Ordinal)
                        ? AttributeSetReason
                        : SchemaReason
                );
                ok = false;
            }
        }

        return ok;
    }

    /// <summary>Reads a switch whose value is <c>yes</c> or <c>no</c>; the value <c>no</c> is the unsupported construct error.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The attribute, such as <c>copy-namespaces</c>.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadSwitch(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string elementName
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(elementName);

        if (!XsltElementReader.TryGetAttribute(element, attributeName, out var raw))
        {
            return true;
        }

        return TryAcceptSwitch(context, element, attributeName, elementName, raw);
    }

    /// <summary>Checks the value of a switch; <c>yes</c> is accepted, <c>no</c> is the unsupported construct error.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The attribute as written in a message.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <param name="raw">The value.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryAcceptSwitch(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string elementName,
        string raw
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(raw);

        var value = raw.Trim();
        if (string.Equals(value, "yes", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(value, "no", StringComparison.Ordinal))
        {
            ReportUnsupported(context, element, attributeName, elementName, NamespaceReason);
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The value '{value}' of the attribute '{attributeName}' of the {elementName} element is neither yes nor no.",
                element,
                FlowCompile.InvalidValueConstraint
            );
        }

        return false;
    }

    /// <summary>Reports an attribute that is the unsupported construct error.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="element">The element.</param>
    /// <param name="attributeName">The attribute as written in a message.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <param name="reason">Why it is not supported.</param>
    internal static void ReportUnsupported(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string elementName,
        string reason
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(reason);

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            $"The attribute '{attributeName}' of the {elementName} element is not supported: {reason}.",
            element
        );
    }
}
