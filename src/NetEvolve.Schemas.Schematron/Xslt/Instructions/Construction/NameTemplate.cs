namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The computed name of <c>xsl:element</c> and <c>xsl:attribute</c> (XSLT 2.0 sections 11.2 and 11.3): the <see langword="name"/> and the optional
/// <see langword="namespace"/> attribute, both attribute value templates. The name is a lexical QName that is expanded with the
/// <see langword="namespace"/> attribute, or else with the namespaces in scope of the instruction; an element without a prefix takes the default
/// namespace in scope, an attribute without a prefix is in no namespace. A name without an expression is expanded once at compile time.
/// </summary>
internal sealed class NameTemplate
{
    private readonly AttributeValueTemplate _name;
    private readonly AttributeValueTemplate? _namespace;
    private readonly XPathNode _instruction;
    private readonly bool _isElement;
    private readonly ExpandedName? _constant;
    private readonly int[] _slots;

    private NameTemplate(
        AttributeValueTemplate name,
        AttributeValueTemplate? namespaceName,
        XPathNode instruction,
        bool isElement
    )
    {
        _name = name;
        _namespace = namespaceName;
        _instruction = instruction;
        _isElement = isElement;
        _slots = new SlotSet()
            .Add(name.ReferencedSlots)
            .Add(namespaceName is null ? default : namespaceName.ReferencedSlots)
            .ToArray();
        if (
            name.IsConstant
            && (namespaceName is null || namespaceName.IsConstant)
            && TryExpand(
                instruction,
                isElement,
                name.Constant!,
                namespaceName?.Constant,
                out var expanded,
                out _,
                out _
            )
        )
        {
            _constant = expanded;
        }
    }

    /// <summary>Gets the variable slots the two templates read.</summary>
    internal ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <summary>Reads the <see langword="name"/> and <see langword="namespace"/> attributes of an element.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="isElement"><see langword="true"/> for <c>xsl:element</c>, <see langword="false"/> for <c>xsl:attribute</c>.</param>
    /// <param name="template">The template when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryCompile(XsltCompileContext context, bool isElement, out NameTemplate template)
    {
        ArgumentNullException.ThrowIfNull(context);

        template = null!;
        var element = context.Element;
        var elementName = isElement ? "xsl:element" : "xsl:attribute";
        var ok = ConstructionCompile.TryReadRequired(context, element, "name", elementName, out var name);
        ok &= ConstructionCompile.TryReadOptional(context, element, "namespace", out var namespaceName);
        if (!ok)
        {
            return false;
        }

        template = new NameTemplate(name, namespaceName, element, isElement);
        return true;
    }

    /// <summary>Evaluates the templates and expands the name.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="location">The location of the instruction, for a problem.</param>
    /// <param name="expanded">The name when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns>The status; the problem is read with <see cref="XsltFrame.TakeProblem"/>.</returns>
    internal QueryStatus TryResolve(XsltFrame frame, in DocumentLocation location, out ExpandedName expanded)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_constant is { } known)
        {
            expanded = known;
            return QueryStatus.Ok;
        }

        expanded = default;
        if (_name.TryEvaluate(frame, location, out var name) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        string? namespaceName = null;
        if (_namespace is not null)
        {
            if (_namespace.TryEvaluate(frame, location, out var text) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            namespaceName = text;
        }

        return TryExpand(
            _instruction,
            _isElement,
            name,
            namespaceName,
            out expanded,
            out var constraint,
            out var message
        )
            ? QueryStatus.Ok
            : ConstructionOutput.Error(frame, location, constraint, message);
    }

    private static bool TryExpand(
        XPathNode instruction,
        bool isElement,
        string name,
        string? namespaceName,
        out ExpandedName expanded,
        out string constraint,
        out string message
    )
    {
        expanded = default;
        constraint = string.Empty;
        var lexical = name.Trim();
        if (!XsltElementReader.TryParseQName(lexical, out var prefix, out var local))
        {
            constraint = isElement ? "XTDE0820" : "XTDE0850";
            message = $"The name '{lexical}' is not a lexical QName.";
            return false;
        }

        if (namespaceName is not null)
        {
            var uri = namespaceName.Trim();
            if (string.Equals(uri, ContentWriter.XmlnsNamespace, StringComparison.Ordinal))
            {
                constraint = isElement ? "XTDE0835" : "XTDE0865";
                message = $"The namespace '{uri}' is not allowed for a constructed name.";
                return false;
            }

            // An empty namespace puts the name in no namespace, whatever its prefix says.
            expanded = new ExpandedName(uri.Length == 0 ? string.Empty : prefix ?? string.Empty, local, uri);
            message = string.Empty;
            return true;
        }

        message = string.Empty;
        if (prefix is null)
        {
            if (!isElement && string.Equals(local, "xmlns", StringComparison.Ordinal))
            {
                constraint = "XTDE0855";
                message = "An attribute cannot be named xmlns without a namespace.";
                return false;
            }

            var defaultNamespace = string.Empty;
            _ = isElement && XsltElementReader.TryResolvePrefix(instruction, string.Empty, out defaultNamespace);
            expanded = new ExpandedName(string.Empty, local, defaultNamespace);
            return true;
        }

        var resolved = ContentWriter.XmlNamespace;
        if (
            !string.Equals(prefix, "xml", StringComparison.Ordinal)
            && !XsltElementReader.TryResolvePrefix(instruction, prefix, out resolved)
        )
        {
            constraint = isElement ? "XTDE0830" : "XTDE0860";
            message =
                $"The prefix '{prefix}' of the name '{lexical}' is not declared in a namespace in scope of the instruction.";
            return false;
        }

        expanded = new ExpandedName(prefix, local, resolved);
        return true;
    }
}
