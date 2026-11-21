namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// An attribute value template (XSLT 2.0 section 5.6): literal text with expressions between curly brackets. The effective value is
/// the literal text with, for each expression, its items atomized and joined by one space (section 5.7.2), evaluated with the focus
/// of the instruction. <c>{{</c> and <c>}}</c> are the escapes of the brackets, and a bracket inside a string literal of an expression
/// does not end it.
/// </summary>
/// <remarks>
/// Limitation: only the atomic kinds that <c>AtomicText</c> converts have a text form (the XPath package offers no casting to string for
/// the others yet, #624); another kind is an evaluation failure. The parser is the first of the package and serves the constructions
/// of this folder; the flow instructions keep constant attributes until #681.
/// </remarks>
internal sealed class AttributeValueTemplate
{
    private readonly Part[] _parts;
    private readonly string? _constant;
    private readonly int[] _slots;

    private AttributeValueTemplate(Part[] parts)
    {
        _parts = parts;
        var builder = new StringBuilder();
        var slots = new SlotSet();
        var constant = true;
        foreach (var part in parts)
        {
            if (part.Expression is { } expression)
            {
                constant = false;
                _ = slots.Add(expression);
            }
            else
            {
                _ = builder.Append(part.Text);
            }
        }

        _constant = constant ? builder.ToString() : null;
        _slots = slots.ToArray();
    }

    /// <summary>Gets a value indicating whether the template has no expression, so its value is known at compile time.</summary>
    internal bool IsConstant => _constant is not null;

    /// <summary>Gets the value of a template without an expression.</summary>
    internal string? Constant => _constant;

    /// <summary>Gets the variable slots the expressions read.</summary>
    internal ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <summary>Compiles the value of an attribute as a template; every problem is reported through the context.</summary>
    /// <param name="context">The compile context; it gives the binding and receives the problem.</param>
    /// <param name="element">The element that has the attribute.</param>
    /// <param name="attributeName">The attribute as written in a message.</param>
    /// <param name="text">The value of the attribute.</param>
    /// <param name="template">The template when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    internal static bool TryCompile(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string text,
        out AttributeValueTemplate template
    )
    {
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(attributeName);
        Argument.ThrowIfNull(text);

        template = null!;
        var parts = new List<Part>();
        var literal = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is not ('{' or '}'))
            {
                _ = literal.Append(c);
            }
            else if (i + 1 < text.Length && text[i + 1] == c)
            {
                _ = literal.Append(c);
                i++;
            }
            else if (c == '}')
            {
                return Report(
                    context,
                    element,
                    attributeName,
                    "has a right curly bracket without a matching left one",
                    "XTSE0370"
                );
            }
            else if (!TryAddExpression(context, element, attributeName, text, ref i, literal, parts))
            {
                return false;
            }
        }

        if (literal.Length > 0)
        {
            parts.Add(new Part(literal.ToString(), null));
        }

        template = new AttributeValueTemplate(parts.ToArray());
        return true;
    }

    /// <summary>Evaluates the template.</summary>
    /// <param name="frame">The frame; its focus is the context of the expressions.</param>
    /// <param name="location">The location of the instruction, for a problem.</param>
    /// <param name="value">The effective value when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns>The status; the problem is read with <see cref="XsltFrame.TakeProblem"/>.</returns>
    internal QueryStatus TryEvaluate(XsltFrame frame, in DocumentLocation location, out string value)
    {
        Argument.ThrowIfNull(frame);

        value = string.Empty;
        if (_constant is not null)
        {
            value = _constant;
            return QueryStatus.Ok;
        }

        var builder = new StringBuilder();
        foreach (var part in _parts)
        {
            if (part.Expression is not { } expression)
            {
                _ = builder.Append(part.Text);
                continue;
            }

            using var text = new TextSink(frame.MaxMessageLength);
            using var join = new JoinSink(text, " ");
            if (frame.Run.TryEvaluate(expression, frame.Focus, join) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            join.Complete();
            if (join.Failure is { } failure)
            {
                return frame.Fail(new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, failure, location));
            }

            if (text.IsTruncated)
            {
                return frame.Fail(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.EvaluationLimitStop,
                        $"The value of an attribute value template is longer than the limit of {text.MaxLength} characters.",
                        location
                    )
                );
            }

            _ = builder.Append(text.ToString());
        }

        value = builder.ToString();
        return QueryStatus.Ok;
    }

    // The expression that starts at the left bracket at the index; the index ends on its right bracket.
    private static bool TryAddExpression(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string text,
        ref int index,
        StringBuilder literal,
        List<Part> parts
    )
    {
        var end = FindEnd(text, index + 1);
        if (end < 0)
        {
            return Report(
                context,
                element,
                attributeName,
                "has a left curly bracket without a matching right one",
                "XTSE0350"
            );
        }

        if (literal.Length > 0)
        {
            parts.Add(new Part(literal.ToString(), null));
            _ = literal.Clear();
        }

        var source = text.Substring(index + 1, end - index - 1);
        if (!FlowCompile.TryCompileExpression(context, source, QueryExpressionKind.Value, out var expression))
        {
            return false;
        }

        parts.Add(new Part(null, expression));
        index = end;
        return true;
    }

    // The end of the expression that starts at the given index: the first right bracket that is not inside a string literal.
    private static int FindEnd(string text, int start)
    {
        var quote = '\0';
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
            }
            else if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == '}')
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Report(
        XsltCompileContext context,
        XPathNode element,
        string attributeName,
        string reason,
        string constraint
    )
    {
        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.AttributeInvalid,
            $"The attribute '{attributeName}' of the element {reason}.",
            element,
            constraint
        );
        return false;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Part(string? text, QueryExpression? expression)
    {
        internal string? Text { get; } = text;

        internal QueryExpression? Expression { get; } = expression;
    }
}
