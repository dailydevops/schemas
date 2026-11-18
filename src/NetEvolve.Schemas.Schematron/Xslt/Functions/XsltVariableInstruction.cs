namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
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
/// Limitations: the content form (a sequence constructor in place of <see langword="select"/>, which builds a temporary tree) is SCH-67; the
/// <see langword="as"/> type is limited to the types the scopes of the package check (<c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and
/// <c>xs:integer</c>, #392) and is checked by the binding through the declared type of the scope; the global <c>xsl:variable</c>
/// belongs to SCH-66 (#252), which extends the positions of this entry together with the registration file. Without select and content the
/// variable is the empty string whether or not <see langword="as"/> is present; XSLT 2.0 section 9.3 gives the empty sequence when
/// <see langword="as"/> is present, so a variable such as <c>as="xs:string"</c> that should fail with XTTE0570 does not (SCH-67).
/// </remarks>
internal sealed class XsltVariableInstruction : IXsltInstruction
{
    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "variable");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var element = context.Element;
        var scope = context.Scope;
        var ok = XsltElementReader.TryReadVariableName(context.Problems, element, "xsl:variable", out var name);
        ok &= TryReadDeclaredType(context, name, out var declared);
        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var select);
        ok &= CheckContent(context, hasSelect);
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

    private static bool TryReadDeclaredType(XsltCompileContext context, string name, out string? declared)
    {
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

    // The value is the select attribute or the content, not both; the content form is not supported yet.
    private static bool CheckContent(XsltCompileContext context, bool hasSelect)
    {
        if (!HasContent(context.Element))
        {
            return true;
        }

        if (hasSelect)
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.ElementMisplaced,
                "An xsl:variable element has either a select attribute or content, not both.",
                context.Element,
                "XTSE0620"
            );
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.XsltUnsupported,
                "The XSLT element 'xsl:variable' with a sequence constructor is not supported; use the select attribute.",
                context.Element
            );
        }

        return false;
    }

    private static bool HasContent(XPathNode element)
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
