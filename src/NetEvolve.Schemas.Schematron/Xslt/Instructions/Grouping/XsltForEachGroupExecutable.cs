namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:for-each-group</c> (XSLT 2.0 section 14.3): evaluates <see langword="select"/> with the focus of the frame into a bounded buffer
/// (the population), assigns the items to groups, orders the groups by the sort keys when there are some, and runs the body once per
/// group. The focus of the body is the initial item of the group, its position among the initial items in processing order, and the
/// number of groups. The current group and the grouping key are bound through <see cref="IXsltGroupRun"/> for every group, also while
/// a sort key is evaluated, and the ones of the caller are put back when the instruction ends, also when it fails. Every group is a
/// cancellation point, as every iteration of <c>xsl:for-each</c> is (design #150 EP-16).
/// </summary>
internal sealed class XsltForEachGroupExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly QueryExpression _select;
    private readonly XsltGrouping _grouping;
    private readonly XsltSortKey[] _keys;
    private readonly XsltBodyExecutable _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltForEachGroupExecutable(
        QueryExpression select,
        XsltGrouping grouping,
        XsltSortKey[] keys,
        XsltBodyExecutable body,
        int slotEnd,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(grouping);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(body);

        _select = select;
        _grouping = grouping;
        _keys = keys;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        var slots = new SlotSet().Add(select).Add(grouping.ReferencedSlots).Add(body);
        foreach (var key in keys)
        {
            _ = slots.Add(key);
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

        if (frame.Run is not IXsltGroupRun run)
        {
            return frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    "The run of the binding cannot hold the current group and the grouping key, which xsl:for-each-group binds.",
                    _location
                )
            );
        }

        using var items = new ItemBuffer(frame.MaxSequenceLength);
        var groups = new List<XsltGroup>();
        if (TryForm(frame, items, groups) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (groups.Count == 0)
        {
            return QueryStatus.Ok;
        }

        // The initial items are the focus of the sort keys and of the body: one item per group, so the size is the number of groups.
        using var initial = new ItemBuffer(groups.Count);
        foreach (var group in groups)
        {
            initial.Item(items.Get(group.Members[0]));
        }

        var outerGroup = run.CurrentGroup;
        var outerKey = run.CurrentGroupingKey;
        var outerFocus = frame.Focus;
        try
        {
            return TryRun(frame, run, sink, new Population(items, initial, groups));
        }
        finally
        {
            frame.Focus = outerFocus;
            run.CurrentGroup = outerGroup;
            run.CurrentGroupingKey = outerKey;
        }
    }

    // Evaluates select into the population and assigns its items to groups.
    private QueryStatus TryForm(XsltFrame frame, ItemBuffer items, List<XsltGroup> groups)
    {
        if (frame.Run.TryEvaluate(_select, frame.Focus, items) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        return items.Overflow
            ? frame.Fail(
                new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    $"The select of xsl:for-each-group has more than the limit of {frame.MaxSequenceLength} items.",
                    _location
                )
            )
            : _grouping.TryGroup(frame, items, groups);
    }

    // Orders the groups and runs the body once per group in processing order.
    private QueryStatus TryRun(XsltFrame frame, IXsltGroupRun run, ISequenceSink sink, Population population)
    {
        var order = Array.Empty<int>();
        if (
            _keys.Length > 0
            && XsltSorter.TrySort(frame, _keys, population.Initial, out order, i =>
                Bind(run, population.Groups[i], population.Items)
            ) == QueryStatus.Problem
        )
        {
            return QueryStatus.Problem;
        }

        for (var position = 0; position < population.Groups.Count; position++)
        {
            frame.CancellationToken.ThrowIfCancellationRequested();
            var index = _keys.Length == 0 ? position : order[position];
            Bind(run, population.Groups[index], population.Items);
            var first = population.Initial.Get(index);
            frame.Focus = QueryFocus.ForItem(in first, position + 1, -1, population.Initial);
            if (_body.TryExecute(frame, sink) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }
        }

        return QueryStatus.Ok;
    }

    private static void Bind(IXsltGroupRun run, XsltGroup group, ItemBuffer items)
    {
        run.CurrentGroup = group.GetSequence(items);
        run.CurrentGroupingKey = group.Key;
    }

    // The population, its groups, and the initial item of every group, which the sort keys and the body take as their focus.
    private readonly struct Population(ItemBuffer items, ItemBuffer initial, List<XsltGroup> groups)
    {
        internal ItemBuffer Items { get; } = items;

        internal ItemBuffer Initial { get; } = initial;

        internal List<XsltGroup> Groups { get; } = groups;
    }
}
