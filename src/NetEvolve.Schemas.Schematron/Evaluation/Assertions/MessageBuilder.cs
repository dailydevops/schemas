namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Builds the text of a compiled message into a <see cref="TextSink"/> without an intermediate string (design #150 EP-7 and EP-8,
/// #137 AP-14): characters are appended as written, a <c>value-of</c> pushes its items into the sink joined by one space, a
/// <c>name</c> appends the name of a node, and <c>emph</c>, <c>dir</c>, and <c>span</c> contribute their content without markup.
/// </summary>
/// <remarks>Not thread-safe: one builder per run. Every expression is evaluated only after <see cref="ISlotBinder"/> bound its slots.</remarks>
internal sealed class MessageBuilder
{
    private readonly IQueryRun _run;
    private readonly ISlotBinder _binder;
    private readonly IProblemSink _problems;
    private readonly ValueOfSink _values = new();
    private readonly CopyOfSink _copies = new();
    private readonly IEmbeddedContent? _embedded;

    /// <summary>Initializes a new instance of the <see cref="MessageBuilder"/> class.</summary>
    /// <param name="run">The run that evaluates the expressions.</param>
    /// <param name="binder">Binds the variables an expression reads.</param>
    /// <param name="problems">Receives the problem of a part that fails.</param>
    /// <param name="embedded">Builds the structured content of a property with <c>xsl:copy-of</c>, or <see langword="null"/> when there is none.</param>
    internal MessageBuilder(IQueryRun run, ISlotBinder binder, IProblemSink problems, IEmbeddedContent? embedded = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(problems);

        _run = run;
        _binder = binder;
        _problems = problems;
        _embedded = embedded;
    }

    /// <summary>Appends the text of a message; a part that fails is reported once and ends the text, which keeps what was built before it.</summary>
    /// <param name="message">The compiled message.</param>
    /// <param name="focus">The context node of the fired rule.</param>
    /// <param name="into">The sink that receives the text.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when a part failed or the text was cut at the maximum length.</returns>
    internal QueryStatus Build(CompiledMessage message, in QueryFocus focus, TextSink into)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(into);

        return Append(message.Parts, in focus, into);
    }

    /// <summary>
    /// Appends the text of a message and, for a property that holds <c>xsl:copy-of</c>, builds its structured content in the same pass:
    /// the nodes that the expression selects and the text of the other parts, in order (SCH-84, #289).
    /// </summary>
    /// <param name="message">The compiled message.</param>
    /// <param name="focus">The context node of the fired rule.</param>
    /// <param name="into">The sink that receives the text.</param>
    /// <param name="content">The snapshot of the content, or <see langword="null"/> when the message has no <c>xsl:copy-of</c>.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when a part failed, the content was not allowed, or the text was cut.</returns>
    internal QueryStatus Build(
        CompiledMessage message,
        in QueryFocus focus,
        TextSink into,
        out IFindingContent? content
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(into);

        content = null;
        if (!message.HasCopyOf)
        {
            return Append(message.Parts, in focus, into);
        }

        if (_embedded is null)
        {
            var missing = new SchematronProblem(
                SchematronDiagnosticCodes.XsltUnsupported,
                "The property holds xsl:copy-of, and the run has no component that builds its content.",
                message.Location
            );
            _problems.Add(in missing);
            return QueryStatus.Problem;
        }

        using var builder = _embedded.BeginFindingContent();
        var status = AppendWithContent(message.Parts, in focus, into, builder);
        if (status != QueryStatus.Ok)
        {
            return status;
        }

        if (!builder.TryComplete(out var completed, out var problem))
        {
            var located = new SchematronProblem(problem.Code, problem.Message, message.Location, problem.Constraint);
            _problems.Add(in located);
            return QueryStatus.Problem;
        }

        content = completed;
        return QueryStatus.Ok;
    }

    private static void AppendName(in XPathNode node, TextSink into)
    {
        var tree = node.Node.Tree;
        if (
            tree is null
            || node.Kind is not (XdmNodeKind.Element or XdmNodeKind.Attribute or XdmNodeKind.ProcessingInstruction)
        )
        {
            return;
        }

        var name = node.Node.Name;
        if (node.Kind != XdmNodeKind.ProcessingInstruction && !name.Prefix.IsEmpty)
        {
            into.Text(tree.Names.GetString(name.Prefix).AsSpan());
            into.Text(":".AsSpan());
        }

        if (!name.Local.IsEmpty)
        {
            into.Text(tree.Names.GetString(name.Local).AsSpan());
        }
    }

    private QueryStatus Append(MessagePart[] parts, in QueryFocus focus, TextSink into)
    {
        for (var i = 0; i < parts.Length; i++)
        {
            var status = AppendPart(parts[i], in focus, into);
            if (status != QueryStatus.Ok)
            {
                return status;
            }
        }

        return QueryStatus.Ok;
    }

    // The parts of a property with xsl:copy-of: each part adds its text to the text of the property and its items to the content.
    private QueryStatus AppendWithContent(
        MessagePart[] parts,
        in QueryFocus focus,
        TextSink into,
        IFindingContentBuilder content
    )
    {
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (IsBlankText(part))
            {
                continue;
            }

            var mark = into.Length;
            var isCopy = part.Kind == MessagePartKind.CopyOf;
            var status = isCopy ? AppendCopyOf(part, in focus, into, content) : AppendPart(part, in focus, into);
            if (status != QueryStatus.Ok)
            {
                return status;
            }

            // A copy-of pushed its items itself; the text of every other part is the text it appended.
            if (!isCopy)
            {
                content.Text(into.Written.Slice(mark));
            }
        }

        return QueryStatus.Ok;
    }

    // White space alone around an xsl:copy-of is formatting, not content (XSLT 2.0 section 4.3): it would end the attributes of the holder.
    private static bool IsBlankText(MessagePart part)
    {
        if (part.Kind != MessagePartKind.Text)
        {
            return false;
        }

        foreach (var c in part.Text)
        {
            if (c is not (' ' or '\t' or '\n' or '\r'))
            {
                return false;
            }
        }

        return true;
    }

    private QueryStatus AppendPart(MessagePart part, in QueryFocus focus, TextSink into)
    {
        var status = QueryStatus.Ok;
        switch (part.Kind)
        {
            case MessagePartKind.Text:
                into.Text(part.Text.AsSpan());
                break;
            case MessagePartKind.ValueOf:
                status = AppendValue(part, in focus, into);
                break;
            case MessagePartKind.Name:
                status = AppendName(part, in focus, into);
                break;
            default:
                status = Append(part.Children, in focus, into);
                break;
        }

        if (status != QueryStatus.Ok)
        {
            return status;
        }

        return CheckLength(part, into);
    }

    private QueryStatus AppendCopyOf(
        MessagePart part,
        in QueryFocus focus,
        TextSink into,
        IFindingContentBuilder content
    )
    {
        var expression = part.Expression!;
        if (_binder.EnsureBound(expression.ReferencedSlots) != QueryStatus.Ok)
        {
            return QueryStatus.Problem;
        }

        // A copy-of that fails leaves nothing of itself in the text; the property is dropped as a whole.
        var mark = into.Length;
        _copies.Begin(into, content);
        if (_run.TryEvaluate(expression, in focus, _copies) != QueryStatus.Ok)
        {
            into.Rewind(mark);
            ReportRun();
            return QueryStatus.Problem;
        }

        if (into.Failure is { } failure)
        {
            into.Rewind(mark);
            Report(failure, part);
            return QueryStatus.Problem;
        }

        return CheckLength(part, into);
    }

    private QueryStatus CheckLength(MessagePart part, TextSink into)
    {
        if (!into.IsTruncated)
        {
            return QueryStatus.Ok;
        }

        Report($"The message is longer than the limit of {into.MaxLength} characters and was cut.", part);
        return QueryStatus.Problem;
    }

    private QueryStatus AppendValue(MessagePart part, in QueryFocus focus, TextSink into)
    {
        var expression = part.Expression!;
        if (_binder.EnsureBound(expression.ReferencedSlots) != QueryStatus.Ok)
        {
            return QueryStatus.Problem;
        }

        // A value-of that fails leaves nothing of itself behind: the items pushed before the failure are dropped.
        var mark = into.Length;
        _values.Begin(into);
        if (_run.TryEvaluate(expression, in focus, _values) != QueryStatus.Ok)
        {
            into.Rewind(mark);
            ReportRun();
            return QueryStatus.Problem;
        }

        if (into.Failure is { } failure)
        {
            into.Rewind(mark);
            Report(failure, part);
            return QueryStatus.Problem;
        }
        return QueryStatus.Ok;
    }

    // Without a path the message holds the name of the context node; with a path it holds the value of the expression, as value-of does.
    private QueryStatus AppendName(MessagePart part, in QueryFocus focus, TextSink into)
    {
        if (part.Expression is null)
        {
            var context = focus.Node;
            AppendName(in context, into);
            return QueryStatus.Ok;
        }

        return AppendValue(part, in focus, into);
    }

    private void ReportRun()
    {
        var problem = _run.GetProblem();
        _problems.Add(in problem);
    }

    private void Report(string message, MessagePart part, string? constraint = null)
    {
        var problem = new SchematronProblem(
            SchematronDiagnosticCodes.EvaluationFailed,
            message,
            part.Location,
            constraint
        );
        _problems.Add(in problem);
    }
}
