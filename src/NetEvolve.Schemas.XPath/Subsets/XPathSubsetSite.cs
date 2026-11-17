namespace NetEvolve.Schemas.XPath.Subsets;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The position in the syntax tree a rule judges: the node plus where it sits in its path.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathSubsetSite
{
    /// <summary>Initializes a new instance of the <see cref="XPathSubsetSite"/> struct.</summary>
    /// <param name="kind">The kind of site.</param>
    /// <param name="node">The node at the site.</param>
    /// <param name="root">The root of the path the site belongs to.</param>
    /// <param name="stepIndex">The zero-based index of the step, 0 for a path or operator site.</param>
    /// <param name="stepCount">The number of steps in the path.</param>
    /// <param name="previous">The step before this one, <see langword="null"/> for the first.</param>
    public XPathSubsetSite(
        XPathSubsetSiteKind kind,
        Expression node,
        PathRoot root,
        int stepIndex,
        int stepCount,
        Expression? previous
    )
    {
        Kind = kind;
        Node = node;
        Root = root;
        StepIndex = stepIndex;
        StepCount = stepCount;
        Previous = previous;
    }

    /// <summary>Gets the kind of site.</summary>
    public XPathSubsetSiteKind Kind { get; }

    /// <summary>Gets the node at the site.</summary>
    public Expression Node { get; }

    /// <summary>Gets the root of the path the site belongs to.</summary>
    public PathRoot Root { get; }

    /// <summary>Gets the zero-based index of the step.</summary>
    public int StepIndex { get; }

    /// <summary>Gets the number of steps in the path.</summary>
    public int StepCount { get; }

    /// <summary>Gets the step before this one, <see langword="null"/> for the first.</summary>
    public Expression? Previous { get; }

    /// <summary>
    /// Gets a value indicating whether the node is the step the parser inserts for <c>//</c>, which is
    /// <c>descendant-or-self::node()</c> located at the two-character operator. The tree keeps no other trace of the
    /// abbreviation (see the limitations on <see cref="XPathSubsetChecker"/>).
    /// </summary>
    public bool IsDescendantSeparator =>
        Node is AxisStep { Axis: Axis.DescendantOrSelf, Test: AnyKindTest, Predicates.Count: 0, Location.Length: 2 };

    /// <summary>Gets a value indicating whether the node is the <c>//</c> of a leading <c>.//</c>.</summary>
    public bool IsLeadingDescendantPrefix =>
        IsDescendantSeparator && StepIndex == 1 && Previous is ContextItemExpression;
}
