namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// The string value of the node that an instruction builds from simple content (XSLT 2.0 section 5.7.2): the result of the
/// <see langword="select"/> expression or of the content, atomized, with the separator between the items. Adjacent text is one item,
/// so the text nodes of a content are joined without a separator.
/// </summary>
internal static class SimpleContent
{
    /// <summary>Evaluates the value of the node.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="select">The expression, or <see langword="null"/> for the content form.</param>
    /// <param name="body">The content, or <see langword="null"/> for the select form.</param>
    /// <param name="separator">The text between two items.</param>
    /// <param name="location">The location of the instruction, for a problem.</param>
    /// <param name="value">The string value when the status is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns>The status; the problem is read with <see cref="XsltFrame.TakeProblem"/>.</returns>
    internal static QueryStatus TryEvaluate(
        XsltFrame frame,
        QueryExpression? select,
        XsltBodyExecutable? body,
        string separator,
        in DocumentLocation location,
        out string value
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(separator);

        value = string.Empty;
        using var text = new TextSink(frame.MaxMessageLength);
        using var join = new JoinSink(text, separator);
        var status = select is not null
            ? frame.Run.TryEvaluate(select, frame.Focus, join)
            : body!.TryExecute(frame, join);
        if (status == QueryStatus.Problem)
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
                    $"The value of a constructed node is longer than the limit of {text.MaxLength} characters.",
                    location
                )
            );
        }

        value = text.ToString();
        return QueryStatus.Ok;
    }
}
