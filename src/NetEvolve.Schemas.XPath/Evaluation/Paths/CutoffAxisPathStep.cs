namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;

/// <summary>An axis step whose first predicate was a positional one that the optimization stage turned into a <see cref="PositionalCutoff"/>.</summary>
internal sealed class CutoffAxisPathStep : AxisPathStep
{
    private readonly PositionalCutoff _cutoff;

    /// <summary>Initializes a new instance of the <see cref="CutoffAxisPathStep"/> class.</summary>
    /// <param name="axis">The axis.</param>
    /// <param name="test">The node test; <c>node()</c> when <paramref name="typed"/> does the matching.</param>
    /// <param name="typed">The matcher of a kind test that carries a type name, <see langword="null"/> for none.</param>
    /// <param name="predicates">The predicates that are evaluated per item, after the cutoff.</param>
    /// <param name="cutoff">The positional predicate that was split off the front of the predicates.</param>
    public CutoffAxisPathStep(
        Axis axis,
        XPathNodeTest test,
        ItemTypeMatcher? typed,
        ExecutableNode[] predicates,
        PositionalCutoff cutoff
    )
        : base(axis, test, typed, predicates) => _cutoff = cutoff;

    /// <inheritdoc />
    public override PositionalCutoff? Cutoff => _cutoff;
}
