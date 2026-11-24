namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An axis step with its node test and predicates (XPath 2.0, 3.2.1 and 3.2.2): for each context node it walks the axis, keeps the
/// nodes that pass the node test, and applies the predicates in the order the axis delivers, which is reverse document order for a
/// reverse axis.
/// </summary>
/// <remarks>
/// The name test is bound to the name table of each tree once per evaluation of the step, not once per node. Limitation: the type
/// name of <c>element(N, T)</c> and <c>attribute(N, T)</c> is checked against the annotation slot, which stays untyped until
/// schema-aware processing exists (#52).
/// </remarks>
internal class AxisPathStep : PathStep
{
    private readonly Axis _axis;
    private readonly XPathNodeTest _test;
    private readonly ItemTypeMatcher _typed;
    private readonly bool _hasType;
    private readonly ExecutableNode[] _predicates;

    /// <summary>Initializes a new instance of the <see cref="AxisPathStep"/> class.</summary>
    /// <param name="axis">The axis.</param>
    /// <param name="test">The node test; <c>node()</c> when <paramref name="typed"/> does the matching.</param>
    /// <param name="typed">The matcher of a kind test that carries a type name, <see langword="null"/> for none.</param>
    /// <param name="predicates">The predicates.</param>
    /// <exception cref="XPathException"><paramref name="axis"/> is the namespace axis (XPST0010).</exception>
    public AxisPathStep(Axis axis, XPathNodeTest test, ItemTypeMatcher? typed, ExecutableNode[] predicates)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)axis, (int)Axis.Child, nameof(axis));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)axis, (int)Axis.AncestorOrSelf, nameof(axis));
        ArgumentNullException.ThrowIfNull(predicates);

        // Fails for the namespace axis before anything else is built.
        _ = XPathAxes.GetProperties(axis);

        _axis = axis;
        _test = test;
        _hasType = typed.HasValue;
        _typed = typed.GetValueOrDefault();
        _predicates = predicates;
    }

    /// <summary>Gets the predicates that are evaluated per item, after the cutoff.</summary>
    public IReadOnlyList<ExecutableNode> Predicates => _predicates;

    /// <summary>Gets the positional predicate that was split off the front of the predicates, <see langword="null"/> for none.</summary>
    /// <remarks>Only <see cref="CutoffAxisPathStep"/> has one, so a step without a cutoff pays nothing for it.</remarks>
    public virtual PositionalCutoff? Cutoff => null;

    /// <summary>Creates the same step with other predicates and a cutoff.</summary>
    /// <param name="predicates">The predicates that are evaluated per item.</param>
    /// <param name="cutoff">The positional predicate that applies before them, <see langword="null"/> for none.</param>
    /// <returns>The step.</returns>
    public AxisPathStep With(ExecutableNode[] predicates, PositionalCutoff? cutoff) =>
        cutoff is { } value
            ? new CutoffAxisPathStep(_axis, _test, _hasType ? _typed : null, predicates, value)
            : new AxisPathStep(_axis, _test, _hasType ? _typed : null, predicates);

    /// <inheritdoc />
    public override bool CanLimit => _predicates.Length == 0 && Cutoff is null;

    /// <inheritdoc />
    public override void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results) =>
        EvaluateFromContext(context, ref results, 0);

    /// <inheritdoc />
    public override void EvaluateFromContext(XPathEvaluationContext context, ref XdmItemList results, int limit)
    {
        ArgumentNullException.ThrowIfNull(context);

        var item = context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        var state = new BindingState();
        var delivered = default(XdmItemList);
        try
        {
            Process(context, item, ref state, ref delivered, ref results, limit);
        }
        finally
        {
            delivered.Dispose();
        }
    }

    /// <inheritdoc />
    public override void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results
    ) => EvaluateOver(context, inputs, ref results, 0);

    /// <inheritdoc />
    public override void EvaluateOver(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmItem> inputs,
        ref XdmItemList results,
        int limit
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var state = new BindingState();
        var delivered = default(XdmItemList);
        try
        {
            for (var i = 0; i < inputs.Length; i++)
            {
                Process(context, inputs[i], ref state, ref delivered, ref results, limit);
                if (limit > 0 && results.Count >= limit)
                {
                    break;
                }
            }
        }
        finally
        {
            delivered.Dispose();
        }
    }

    /// <inheritdoc />
    public override XPathOrderProperties Compose(XPathOrderProperties input) => XPathOrderAnalysis.Step(input, _axis);

    private void Process(
        XPathEvaluationContext context,
        in XdmItem input,
        ref BindingState state,
        ref XdmItemList delivered,
        ref XdmItemList results,
        int limit
    )
    {
        if (!input.TryGetNode(out var node))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0020);
        }

        var tree = node.Tree;
        if (!ReferenceEquals(tree, state.Tree))
        {
            state.Tree = tree;
            state.Matcher = _test.Bind(tree!.Names);
        }

        // The walk stops once the cutoff or the limit has what it needs; the limit applies to a step without predicates only.
        var stop =
            Cutoff is { } cutoff ? cutoff.Budget
            : limit > 0 ? limit - results.Count
            : 0;
        delivered.Clear();
        var iterator = new XPathAxisIterator(node, _axis, state.Matcher);
        while (iterator.MoveNext())
        {
            var current = XdmItem.FromNode(iterator.Current);
            if (
                !_hasType
                || _typed.Matches(
                    current,
                    UntypedAnnotationSource.Instance,
                    ref state.TypedTree,
                    ref state.TypedMatcher
                )
            )
            {
                delivered.Add(current);
                if (stop > 0 && delivered.Count >= stop)
                {
                    break;
                }
            }
        }

        Cutoff?.Apply(ref delivered);
        if (_predicates.Length > 0)
        {
            PredicateFilter.Apply(context, _predicates, ref delivered);
        }

        results.AddRange(delivered.Items);
    }

    // The tree the matchers are bound for; a step over nodes of one tree binds once.
    private struct BindingState
    {
        public XmlTree? Tree;
        public XPathNodeMatcher Matcher;
        public XmlTree? TypedTree;
        public XPathNodeMatcher TypedMatcher;
    }
}
