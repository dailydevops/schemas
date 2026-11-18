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
    private readonly NodeSink _nodes = new();

    /// <summary>Initializes a new instance of the <see cref="MessageBuilder"/> class.</summary>
    /// <param name="run">The run that evaluates the expressions.</param>
    /// <param name="binder">Binds the variables an expression reads.</param>
    /// <param name="problems">Receives the problem of a part that fails.</param>
    internal MessageBuilder(IQueryRun run, ISlotBinder binder, IProblemSink problems)
    {
        Argument.ThrowIfNull(run);
        Argument.ThrowIfNull(binder);
        Argument.ThrowIfNull(problems);

        _run = run;
        _binder = binder;
        _problems = problems;
    }

    /// <summary>Appends the text of a message; a part that fails is reported once and ends the text, which keeps what was built before it.</summary>
    /// <param name="message">The compiled message.</param>
    /// <param name="focus">The context node of the fired rule.</param>
    /// <param name="into">The sink that receives the text.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when a part failed or the text was cut at the maximum length.</returns>
    internal QueryStatus Build(CompiledMessage message, in QueryFocus focus, TextSink into)
    {
        Argument.ThrowIfNull(message);
        Argument.ThrowIfNull(into);

        return Append(message.Parts, in focus, into);
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
            var part = parts[i];
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

            if (into.IsTruncated)
            {
                Report($"The message is longer than the limit of {into.MaxLength} characters and was cut.", part);
                return QueryStatus.Problem;
            }
        }

        return QueryStatus.Ok;
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

    private QueryStatus AppendName(MessagePart part, in QueryFocus focus, TextSink into)
    {
        if (part.Expression is not { } path)
        {
            var context = focus.Node;
            AppendName(in context, into);
            return QueryStatus.Ok;
        }

        if (_binder.EnsureBound(path.ReferencedSlots) != QueryStatus.Ok)
        {
            return QueryStatus.Problem;
        }

        _nodes.Reset();
        if (_run.TryEvaluate(path, in focus, _nodes) != QueryStatus.Ok)
        {
            ReportRun();
            return QueryStatus.Problem;
        }

        if (_nodes.Count == 0)
        {
            return QueryStatus.Ok;
        }

        if (_nodes.Count > 1 || !_nodes.FirstIsNode)
        {
            Report("The path of a name element must select at most one node.", part, "XPTY0004");
            return QueryStatus.Problem;
        }

        AppendName(_nodes.First, into);
        return QueryStatus.Ok;
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
