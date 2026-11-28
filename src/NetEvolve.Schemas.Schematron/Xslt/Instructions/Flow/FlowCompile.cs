namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The compile steps that the flow instructions share: an expression, a body with its own variable scope, a branch with a test, and
/// an attribute whose value is a constant. Every problem is reported through the context; the compile continues after one.
/// </summary>
/// <remarks>
/// Limitation: an attribute value template (XSLT 2.0 section 5.6) is not evaluated. The attributes of the flow instructions that are
/// templates in XSLT 2.0 (<c>separator</c>, <c>terminate</c>, <c>order</c>, <c>stable</c>, <c>data-type</c>, <c>collation</c>,
/// <c>lang</c>, and <c>case-order</c>) accept a value without an expression; the escapes <c>{{</c> and <c>}}</c> are read, and an
/// expression between curly brackets is reported as not supported (#681).
/// </remarks>
internal static class FlowCompile
{
    internal const string MisplacedConstraint = "XTSE0010";
    internal const string InvalidValueConstraint = "XTSE0020";

    /// <summary>Compiles an expression in a scope.</summary>
    /// <param name="context">The compile context; it gives the binding and receives the problem.</param>
    /// <param name="text">The expression.</param>
    /// <param name="kind">What the expression is used for.</param>
    /// <param name="expression">The expression when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the expression compiled.</returns>
    internal static bool TryCompileExpression(
        XsltCompileContext context,
        string text,
        QueryExpressionKind kind,
        out QueryExpression expression
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(text);

        var binding = context.Binding
            ?? throw new InvalidOperationException(
                "The compiler of the context gave no query binding for an expression."
            );
        if (binding.TryCompile(text.AsSpan(), kind, context.Scope, out expression, out var found))
        {
            return true;
        }

        context.Problems.Add(in found);
        return false;
    }

    /// <summary>
    /// Compiles a sequence constructor in a scope of its own: the local variables of the body are visible to the later siblings in
    /// the body and nowhere else, and take the slots above the variables visible at the instruction.
    /// </summary>
    /// <param name="context">The compile context of the instruction.</param>
    /// <param name="first">The first node of the content; an empty node for none.</param>
    /// <param name="body">The compiled body when the result is <see langword="true"/>.</param>
    /// <param name="slotEnd">The first slot above every slot the body uses; valid also when the compile failed.</param>
    /// <returns><see langword="true"/> when every child compiled.</returns>
    internal static bool TryCompileBody(
        XsltCompileContext context,
        XmlNode first,
        out XsltBodyExecutable body,
        out int slotEnd
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var names = new List<string>(context.Scope.VariableNames);
        var ok = XsltSequenceConstructor.TryCompile(context, first, names, out body);
        slotEnd = names.Count;
        return ok;
    }

    /// <summary>Reads an <c>xsl:if</c> or <c>xsl:when</c>: a required <c>test</c> and a body.</summary>
    /// <param name="context">The compile context; its scope is the scope the element sits in.</param>
    /// <param name="element">The element.</param>
    /// <param name="elementName">The element as written in a message, such as <c>xsl:when</c>.</param>
    /// <param name="branch">The compiled branch when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the element compiled.</returns>
    internal static bool TryReadBranch(
        XsltCompileContext context,
        XPathNode element,
        string elementName,
        out XsltIfExecutable branch
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(elementName);

        branch = null!;
        QueryExpression? test = null;
        bool ok;
        if (XsltElementReader.TryGetAttribute(element, "test", out var text))
        {
            ok = TryCompileExpression(context, text, QueryExpressionKind.Boolean, out test);
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                $"The {elementName} element has no test attribute.",
                element
            );
            ok = false;
        }

        ok &= TryCompileBody(context, element.Node.FirstChild, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        branch = new XsltIfExecutable(test!, body, slotEnd);
        return true;
    }

    /// <summary>Reads an attribute whose value must be a constant (see the limitation of the class).</summary>
    /// <param name="context">The compile context; it receives the problem.</param>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="elementName">The element as written in a message, such as <c>xsl:sort</c>.</param>
    /// <param name="value">The value with the curly bracket escapes resolved, or <see langword="null"/> when the attribute is absent.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadConstant(
        XsltCompileContext context,
        XPathNode element,
        string name,
        string elementName,
        out string? value
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(elementName);

        value = null;
        if (!XsltElementReader.TryGetAttribute(element, name, out var raw))
        {
            return true;
        }

        if (raw.AsSpan().IndexOfAny('{', '}') < 0)
        {
            value = raw;
            return true;
        }

        var found = Unescape(raw, out var unescaped);
        if (found == Template.None)
        {
            value = unescaped;
            return true;
        }

        var (code, message, constraint) = found switch
        {
            Template.UnescapedRight => (
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The attribute '{name}' of the {elementName} element has an unescaped right curly bracket.",
                "XTSE0370"
            ),
            Template.UnmatchedLeft => (
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The attribute '{name}' of the {elementName} element has a left curly bracket without a matching right one.",
                "XTSE0350"
            ),
            _ => (
                SchematronDiagnosticCodes.XsltUnsupported,
                $"The attribute value template in the attribute '{name}' of the {elementName} element is not supported; use a value without an expression.",
                (string?)null
            ),
        };
        Report(context, code, message, element, constraint);
        return false;
    }

    // Resolves {{ and }} and tells what else the value holds: a lone right bracket, a left bracket that never closes, or an expression.
    private static Template Unescape(string raw, out string value)
    {
        var builder = new StringBuilder(raw.Length);
        value = string.Empty;
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (c is not ('{' or '}'))
            {
                _ = builder.Append(c);
            }
            else if (i + 1 < raw.Length && raw[i + 1] == c)
            {
                _ = builder.Append(c);
                i++;
            }
            else if (c == '}')
            {
                return Template.UnescapedRight;
            }
            else
            {
                return raw.AsSpan(i).IndexOf('}') < 0 ? Template.UnmatchedLeft : Template.Expression;
            }
        }

        value = builder.ToString();
        return Template.None;
    }

    private enum Template
    {
        None,
        UnescapedRight,
        UnmatchedLeft,
        Expression,
    }

    /// <summary>Reads a constant attribute that must be one of a few words.</summary>
    /// <param name="context">The compile context; it receives the problem.</param>
    /// <param name="element">The element.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="elementName">The element as written in a message.</param>
    /// <param name="allowed">The permitted values.</param>
    /// <param name="value">The value without surrounding white space, or <see langword="null"/> when the attribute is absent.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryReadChoice(
        XsltCompileContext context,
        XPathNode element,
        string name,
        string elementName,
        string[] allowed,
        out string? value
    )
    {
        ArgumentNullException.ThrowIfNull(allowed);

        if (!TryReadConstant(context, element, name, elementName, out value))
        {
            return false;
        }

        if (value is null)
        {
            return true;
        }

        value = value.Trim();
        if (Array.IndexOf(allowed, value) >= 0)
        {
            return true;
        }

        Report(
            context,
            SchematronDiagnosticCodes.AttributeInvalid,
            $"The value '{value}' of the attribute '{name}' of the {elementName} element is none of: {string.Join(", ", allowed)}.",
            element,
            InvalidValueConstraint
        );
        return false;
    }

    internal static void Report(
        XsltCompileContext context,
        DiagnosticCode code,
        string message,
        XPathNode element,
        string? constraint
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        XsltProblems.Report(context.Problems, code, message, element, constraint);
    }
}
