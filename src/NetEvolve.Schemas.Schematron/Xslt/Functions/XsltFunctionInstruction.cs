namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:function</c> (XSLT 2.0 section 10.3, design #150 EP-9 and EP-13): a top-level declaration with a prefixed name, an optional
/// <see langword="as"/> type, <c>xsl:param</c> children, and a sequence constructor. The compile scope of the element is the function scope: the
/// schema-level names, padded with empty entries up to the first slot a function may use (above every slot of the schema, phase,
/// pattern, and rule blocks), so a body reads schema-level variables and its own parameters and locals, and nothing of a phase,
/// pattern, or rule. Every problem is reported through the context and the compile continues after one; a failed compile returns
/// <see langword="false"/> with a default problem, which means that the problems are already reported.
/// </summary>
/// <remarks>
/// Limitations: <see langword="override"/> is read and kept, but the precedence rule of an imported function is applied by the table with the
/// import precedence of the module (SCH-65, #286); the schema-aware types of <see langword="as"/> await DEF-11 (#23).
/// </remarks>
internal sealed class XsltFunctionInstruction : IXsltInstruction
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
    private const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    private const string ParamLocalName = "param";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "function");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Declaration;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var ok = TryReadName(context, out var name, out var written);
        ok &= TryReadResultType(context, written, out var result);
        ok &= TryReadOverride(context, written, out var overridesImported);

        var names = new List<string>(context.Scope.VariableNames);
        var slotBase = names.Count;
        var parameters = new List<XsltFunctionParameter>();
        var bodyStart = ReadHead(context, names, parameters, out var headOk);
        ok &= headOk;
        ok &= XsltSequenceConstructor.TryCompile(context, bodyStart, names, out var body);
        if (!ok)
        {
            return false;
        }

        var component = new XsltFunctionComponent(
            name,
            written,
            parameters,
            result,
            body,
            slotBase,
            names.Count - slotBase,
            SchemaSlots(body, slotBase),
            overridesImported,
            XsltElementResolver.LocationOf(context.Element, null)
        );
        executable = new XsltFunctionDeclaration(component);
        return true;
    }

    // The slots of the schema level that the body reads; the slots of the function itself are bound by the call.
    private static int[] SchemaSlots(XsltBodyExecutable body, int slotBase)
    {
        var slots = new List<int>();
        foreach (var slot in body.ReferencedSlots)
        {
            if (slot < slotBase)
            {
                slots.Add(slot);
            }
        }

        return slots.ToArray();
    }

    private static bool TryReadResultType(XsltCompileContext context, string written, out XsltSequenceType? result)
    {
        result = null;
        var element = context.Element;
        if (!XsltElementReader.TryGetAttribute(element, "as", out var typeText))
        {
            return true;
        }

        if (
            XsltSequenceType.TryParse(
                typeText,
                XsltElementReader.StaticContext(element),
                out result,
                out var diagnostic
            )
        )
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.ExpressionInvalid,
            $"The type '{typeText}' of the function '{written}' is not valid: {diagnostic.Message}",
            element,
            diagnostic.Code
        );
        return false;
    }

    private static bool TryReadOverride(XsltCompileContext context, string written, out bool overridesImported)
    {
        overridesImported = true;
        if (!XsltElementReader.TryGetAttribute(context.Element, "override", out var text))
        {
            return true;
        }

        if (string.Equals(text, "no", StringComparison.Ordinal))
        {
            overridesImported = false;
            return true;
        }

        if (string.Equals(text, "yes", StringComparison.Ordinal))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.AttributeInvalid,
            $"The value '{text}' of the override attribute of the function '{written}' is neither yes nor no.",
            context.Element,
            "XTSE0020"
        );
        return false;
    }

    // Reads the xsl:param children at the start of the content and returns the first node of the body.
    private static XmlNode ReadHead(
        XsltCompileContext context,
        List<string> names,
        List<XsltFunctionParameter> parameters,
        out bool ok
    )
    {
        ok = true;
        var child = context.Element.Node.FirstChild;
        for (; !child.IsEmpty; child = child.NextSibling)
        {
            if (child.Kind == XmlNodeKind.Element && XsltElementReader.IsXslt(child, ParamLocalName))
            {
                ok &= ReadParameter(context, new XPathNode(child), names, parameters);
            }
            else if (
                child.Kind == XmlNodeKind.Element
                || (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue))
            )
            {
                break;
            }
        }

        return child;
    }

    private static bool ReadParameter(
        XsltCompileContext context,
        XPathNode element,
        List<string> names,
        List<XsltFunctionParameter> parameters
    )
    {
        // A failed parameter still takes its slot, so the slots of the later ones stay where the compile expects them.
        if (!XsltParamInstruction.TryRead(context, element, names.Count, out var parameter))
        {
            names.Add(XsltElementReader.NameOrEmpty(element));
            return false;
        }

        foreach (var earlier in parameters)
        {
            if (string.Equals(earlier.Name, parameter.Name, StringComparison.Ordinal))
            {
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.LetDuplicate,
                    $"The function has two parameters named '{parameter.Name}'.",
                    element,
                    "XTSE0580"
                );
                names.Add(parameter.Name);
                return false;
            }
        }

        names.Add(parameter.Name);
        parameters.Add(parameter);
        return true;
    }

    private static bool TryReadName(XsltCompileContext context, out XPathQName name, out string written)
    {
        name = default;
        var element = context.Element;
        if (!XsltElementReader.TryGetAttribute(element, "name", out written))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:function element has no name attribute.",
                element
            );
            return false;
        }

        if (!TryExpand(element, written, out var expanded, out var constraint, out var reason))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The name '{written}' of the function {reason}.",
                element,
                constraint
            );
            return false;
        }

        name = expanded;
        return true;
    }

    // A stylesheet function needs a prefixed name in a namespace of its own (XTSE0740, XTSE0280, XTSE0080).
    private static bool TryExpand(
        XPathNode element,
        string written,
        out XPathQName name,
        out string? constraint,
        out string? reason
    )
    {
        name = default;
        constraint = null;
        reason = null;
        if (!XsltElementReader.TryParseQName(written, out var prefix, out var local))
        {
            constraint = "XTSE0020";
            reason = "is not a QName";
        }
        else if (prefix is null)
        {
            constraint = "XTSE0740";
            reason =
                "has no prefix, which a stylesheet function needs to stay apart from the functions of the default namespace";
        }
        else if (!XsltElementReader.TryResolvePrefix(element, prefix, out var uri))
        {
            constraint = "XTSE0280";
            reason = $"uses the prefix '{prefix}', which is not bound to a namespace";
        }
        else if (IsReserved(uri))
        {
            constraint = "XTSE0080";
            reason = $"is in the reserved namespace '{uri}'";
        }
        else
        {
            name = new XPathQName(uri, local);
        }

        return reason is null;
    }

    private static bool IsReserved(string namespaceName) =>
        string.Equals(namespaceName, XsltInstructionRegistry.Namespace, StringComparison.Ordinal)
        || string.Equals(namespaceName, FunctionNamespace, StringComparison.Ordinal)
        || string.Equals(namespaceName, XmlNamespace, StringComparison.Ordinal)
        || string.Equals(namespaceName, XmlSchemaNamespace, StringComparison.Ordinal)
        || string.Equals(namespaceName, XmlSchemaInstanceNamespace, StringComparison.Ordinal);
}
