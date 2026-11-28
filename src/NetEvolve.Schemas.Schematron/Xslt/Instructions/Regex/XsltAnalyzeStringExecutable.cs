namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Regex;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>xsl:analyze-string</c> (XSLT 2.0 section 15.1): evaluates <see langword="select"/> to one string, compiles the regex with its
/// flags through the shared pattern cache of the XPath package, and walks the input once with the <see cref="MatchSegmentEnumerator"/>
/// of that package, which yields the matching and the non-matching substrings in input order. Then the body of the matching or the
/// non-matching substring runs once per substring with the substring as the context item, its position among all substrings, and
/// their number. The groups of a matching substring are bound through <see cref="IXsltRegexRun"/> for <c>regex-group()</c>, and the
/// ones of the caller are put back when the instruction ends, also when it fails. Every substring is a cancellation point
/// (design #150 EP-16).
/// </summary>
internal sealed class XsltAnalyzeStringExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression _select;
    private readonly AttributeValueTemplate _regex;
    private readonly AttributeValueTemplate? _flags;
    private readonly XsltBodyExecutable? _matching;
    private readonly XsltBodyExecutable? _nonMatching;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltAnalyzeStringExecutable(
        QueryExpression select,
        AttributeValueTemplate regex,
        AttributeValueTemplate? flags,
        XsltBodyExecutable? matching,
        XsltBodyExecutable? nonMatching,
        int slotEnd,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(regex);

        _select = select;
        _regex = regex;
        _flags = flags;
        _matching = matching;
        _nonMatching = nonMatching;
        _location = location;
        SlotEnd = slotEnd;
        var slots = new SlotSet().Add(select).Add(regex.ReferencedSlots).Add(matching).Add(nonMatching);
        if (flags is not null)
        {
            _ = slots.Add(flags.ReferencedSlots);
        }

        _slots = slots.ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        if (frame.Run is not IXsltRegexRun run)
        {
            return Fail(
                frame,
                SchematronDiagnosticCodes.EvaluationFailed,
                "The run of the binding cannot hold the regex groups, which xsl:analyze-string binds."
            );
        }

        if (TryReadInput(frame, out var input) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (TryCompile(frame, out var operations) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        using var substrings = new ItemBuffer(frame.MaxSequenceLength);
        var groups = new List<XPathRegexGroups?>();
        if (TryDivide(frame, operations!, input, substrings, groups) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var outerGroups = run.RegexGroups;
        var outerFocus = frame.Focus;
        try
        {
            for (var i = 0; i < substrings.Count; i++)
            {
                frame.CancellationToken.ThrowIfCancellationRequested();
                var body = groups[i] is null ? _nonMatching : _matching;
                if (body is null)
                {
                    continue;
                }

                var item = substrings.Get(i);
                run.RegexGroups = groups[i];
                frame.Focus = QueryFocus.ForItem(in item, i + 1, -1, substrings);
                if (body.TryExecute(frame, sink) == QueryStatus.Problem)
                {
                    return QueryStatus.Problem;
                }
            }

            return QueryStatus.Ok;
        }
        finally
        {
            frame.Focus = outerFocus;
            run.RegexGroups = outerGroups;
        }
    }

    // The input is exactly one string; a node is atomized to its string value.
    private QueryStatus TryReadInput(XsltFrame frame, out string input)
    {
        input = string.Empty;
        using var items = new ItemBuffer(2);
        if (frame.Run.TryEvaluate(_select, frame.Focus, items) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (items.Count != 1 || items.Overflow)
        {
            return Fail(
                frame,
                SchematronDiagnosticCodes.EvaluationFailed,
                "The select of xsl:analyze-string gave no value or more than one value (XTTE1100).",
                "XTTE1100"
            );
        }

        var item = items.Get(0);
        if (item.TryGetNode(out var node))
        {
            input = node.Node.StringValue;
            return QueryStatus.Ok;
        }

        if (item.TryGetString(out var text) && text is not null)
        {
            input = text;
            return QueryStatus.Ok;
        }

        return Fail(
            frame,
            SchematronDiagnosticCodes.EvaluationFailed,
            "The select of xsl:analyze-string is not a string (XTTE1100).",
            "XTTE1100"
        );
    }

    // The regex and the flags are templates, so they are known only now; a constant template is one string instance and is compiled once.
    private QueryStatus TryCompile(XsltFrame frame, out RegexOperations? operations)
    {
        operations = null;
        if (_regex.TryEvaluate(frame, in _location, out var regex) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var flags = string.Empty;
        if (_flags is not null && _flags.TryEvaluate(frame, in _location, out flags) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        try
        {
            operations = PatternCache.Shared.Get(regex, flags);
        }
        catch (XPathException failure)
        {
            var code = failure.Diagnostic.Code.ToString();
            return string.Equals(code, "FORX0001", StringComparison.Ordinal)
                    ? Fail(frame, SchematronDiagnosticCodes.EvaluationFailed, failure.Message, "XTDE1145")
                : string.Equals(code, "FORX0002", StringComparison.Ordinal)
                    ? Fail(frame, SchematronDiagnosticCodes.EvaluationFailed, failure.Message, "XTDE1140")
                : Fail(frame, SchematronDiagnosticCodes.EvaluationLimitStop, failure.Message);
        }

        return operations.MatchesEmptyString
            ? Fail(
                frame,
                SchematronDiagnosticCodes.EvaluationFailed,
                "The regex of xsl:analyze-string matches the zero-length string (XTDE1150).",
                "XTDE1150"
            )
            : QueryStatus.Ok;
    }

    // One walk of the iterator: the text of every substring, and the groups of every matching one (null for a non-matching one).
    private QueryStatus TryDivide(
        XsltFrame frame,
        RegexOperations operations,
        string input,
        ItemBuffer substrings,
        List<XPathRegexGroups?> groups
    )
    {
        var segments = new MatchSegmentEnumerator(operations, input.AsSpan());
        try
        {
            while (segments.MoveNext())
            {
                frame.CancellationToken.ThrowIfCancellationRequested();
                var segment = segments.Current;
                var item = XdmItem.FromString(input.Substring(segment.Index, segment.Length));
                substrings.Item(in item);
                if (substrings.Overflow)
                {
                    return Fail(
                        frame,
                        SchematronDiagnosticCodes.EvaluationLimitStop,
                        $"The input of xsl:analyze-string has more than the limit of {frame.MaxSequenceLength} substrings."
                    );
                }

                groups.Add(segment.Kind == MatchSegmentKind.Match ? GroupsOf(segment, input) : null);
            }

            return segments.IsAborted
                ? Fail(
                    frame,
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    "The search of xsl:analyze-string reached a step ceiling of the regex engine."
                )
                : QueryStatus.Ok;
        }
        finally
        {
            segments.Dispose();
        }
    }

    private static XPathRegexGroups GroupsOf(MatchSegment segment, string input)
    {
        var ranges = new TextRange[segment.GroupCount + 1];
        for (var number = 0; number < ranges.Length; number++)
        {
            _ = segment.TryGetGroup(number, out ranges[number]);
        }

        return new XPathRegexGroups(input, ranges);
    }

    private QueryStatus Fail(XsltFrame frame, DiagnosticCode code, string message, string? constraint = null) =>
        frame.Fail(new SchematronProblem(code, message, _location, constraint));
}
