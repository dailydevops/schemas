namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
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
/// exactly the variables declared before it; the caller of the sequence constructor adds the name for the later siblings.
/// </summary>
/// <remarks>
/// Limitations: the content form (a sequence constructor in place of <see langword="select"/>, which builds a temporary tree) is left out (#629, #794); the
/// <see langword="as"/> type is limited to the types the scopes of the package check (<c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and
/// <c>xs:integer</c>, #392) and is checked by the binding through the declared type of the scope. The entry is also the global
/// <c>xsl:variable</c> (SCH-66, #252): a top-level element, which has the schema or a stylesheet module as its parent, is read by
/// <see cref="XsltGlobalCompile"/> and compiled by the planner of the scopes. Without select and content the
/// variable is the empty string whether or not <see langword="as"/> is present; XSLT 2.0 section 9.3 gives the empty sequence when
/// <see langword="as"/> is present, so a variable such as <c>as="xs:string"</c> that should fail with XTTE0570 does not (#629).
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
        ok &= TryReadDeclaredType(context, name, out var declared);
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var select);
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
