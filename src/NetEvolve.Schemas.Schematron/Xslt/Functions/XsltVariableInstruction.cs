namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A local <c>xsl:variable</c> in a sequence constructor (XSLT 2.0 section 9): a name, an optional <see langword="as"/> type, and a <see langword="select"/>
/// expression. The variable takes the next slot of the scope it is compiled in, so the compile scope of the instruction names
/// exactly the variables declared before it; the caller of the sequence constructor adds the name for the later siblings. A sequence
/// constructor in place of <see langword="select"/> builds a temporary tree with a document node, or, with <see langword="as"/>, the
/// constructed sequence after the function conversion rules (<see cref="XsltVariableContentExecutable"/>, #629).
/// </summary>
/// <remarks>
/// Limitations: the global form with content is left out (#794); with <see langword="select"/> the
/// <see langword="as"/> type is limited to the types the scopes of the package check (<c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and
/// <c>xs:integer</c>, #392) and is checked by the binding through the declared type of the scope. The entry is also the global
/// <c>xsl:variable</c> (SCH-66, #252): a top-level element, which has the schema or a stylesheet module as its parent, is read by
/// <see cref="XsltGlobalCompile"/> and compiled by the planner of the scopes. Without select and content a local
/// variable is the empty string, or the empty sequence converted to <see langword="as"/> when that is present (XSLT 2.0 section 9.3).
/// </remarks>
internal sealed class XsltVariableInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "variable");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction | XsltPosition.Declaration;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        if (XsltGlobalCompile.IsTopLevel(context.Element))
        {
            return XsltGlobalCompile.TryCompile(context, false, out executable);
        }

        executable = null!;
        var element = context.Element;
        var scope = context.Scope;
        var ok = XsltElementReader.TryReadVariableName(context.Problems, element, "xsl:variable", out var name);
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var select);
        if (!hasSelect && (HasContent(element) || XsltElementReader.TryGetAttribute(element, "as", out _)))
        {
            return TryCompileContent(context, name, ok, out executable);
        }

        ok &= TryReadDeclaredType(context, name, out var declared);
        ok &= CheckContent(context, hasSelect, "xsl:variable");
        if (!ok)
        {
            return false;
        }

        var binding = context.Binding
            ?? throw new InvalidOperationException(
                "The compiler of the context gave no query binding for an expression."
            );
        var valueScope = new QueryStaticScope(
            scope.Namespaces,
            scope.VariableNames,
            scope.BaseUri,
            scope.Edition,
            declared
        );
        var text = hasSelect ? select : "''";
        if (!binding.TryCompile(text.AsSpan(), QueryExpressionKind.Value, valueScope, out var value, out var found))
        {
            context.Problems.Add(in found);
            return false;
        }

        executable = new XsltVariableExecutable(name, scope.VariableNames.Count, value);
        return true;
    }

    // The content form: the sequence constructor is compiled with the slot of the variable named by an empty name, so that the
    // locals of the content take the slots above it and the variable does not see itself.
    private static bool TryCompileContent(
        XsltCompileContext context,
        string name,
        bool ok,
        out XsltExecutable executable
    )
    {
        executable = null!;
        var element = context.Element;
        XsltSequenceType? type = null;
        if (
            XsltElementReader.TryGetAttribute(element, "as", out var typeText)
            && !XsltSequenceType.TryParse(typeText, XsltElementReader.StaticContext(element), out type, out var found)
        )
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.ExpressionInvalid,
                $"The type '{typeText}' of the variable '{name}' is not valid: {found.Message}",
                element,
                found.Code
            );
            ok = false;
        }

        var names = new List<string>(context.Scope.VariableNames) { string.Empty };
        ok &= XsltSequenceConstructor.TryCompile(context, element.Node.FirstChild, names, out var body);
        if (!ok)
        {
            return false;
        }

        executable = new XsltVariableContentExecutable(
            name,
            context.Scope.VariableNames.Count,
            type,
            body,
            XsltElementResolver.LocationOf(element, null),
            names.Count
        );
        return true;
    }

    /// <summary>Reads the <see langword="as"/> type of a variable or parameter; an unsupported type is reported.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="name">The name as written, for the message.</param>
    /// <param name="declared">The canonical type name, or <see langword="null"/> when there is no type.</param>
    /// <returns><see langword="false"/> when the type is not supported.</returns>
    internal static bool TryReadDeclaredType(XsltCompileContext context, string name, out string? declared)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(name);

        declared = null;
        if (
            !XsltElementReader.TryGetAttribute(context.Element, "as", out var typeText)
            || ConstantValue.TryGetSupportedType(typeText, context.Scope.Namespaces, out declared)
        )
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.Unsupported,
            $"The type '{typeText}' of the variable '{name}' is not supported; only xs:string, xs:boolean, xs:double, and xs:integer are checked.",
            context.Element
        );
        return false;
    }

    /// <summary>The value is the select attribute or the content, not both; the content form is not supported yet.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="hasSelect">Whether the element has a <see langword="select"/> attribute.</param>
    /// <param name="elementName">The element as written in a message, such as <c>xsl:variable</c>.</param>
    /// <returns><see langword="false"/> when the element has content and a problem was reported.</returns>
    internal static bool CheckContent(XsltCompileContext context, bool hasSelect, string elementName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(elementName);

        if (!HasContent(context.Element))
        {
            return true;
        }

        if (hasSelect)
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.ElementMisplaced,
                $"An {elementName} element has either a select attribute or content, not both.",
                context.Element,
                "XTSE0620"
            );
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.XsltUnsupported,
                $"The XSLT element '{elementName}' with a sequence constructor is not supported; use the select attribute.",
                context.Element
            );
        }

        return false;
    }

    /// <summary>Determines whether an element has content other than white space.</summary>
    /// <param name="element">The element.</param>
    /// <returns><see langword="true"/> when it has a child element or text that is not white space.</returns>
    internal static bool HasContent(XPathNode element)
    {
        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (
                child.Kind == XmlNodeKind.Element
                || (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue))
            )
            {
                return true;
            }
        }

        return false;
    }
}
