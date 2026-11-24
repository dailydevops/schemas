namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Axis step (productions AxisStep, ForwardStep, ReverseStep, and the abbreviated forms).</summary>
internal sealed class AxisStep : Expression
{
    /// <summary>Initializes a new instance of the <see cref="AxisStep"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="axis">Gets the axis.</param>
    /// <param name="test">Gets the test.</param>
    /// <param name="predicates">Gets the predicates (productions PredicateList and Predicate).</param>
    public AxisStep(SourceSpan location, Axis axis, NodeTest test, IReadOnlyList<Expression> predicates)
        : base(location)
    {
        Axis = axis;
        Test = test;
        Predicates = NodeList.Copy(predicates);
    }

    /// <summary>Gets the axis.</summary>
    public Axis Axis { get; }

    /// <summary>Gets the test.</summary>
    public NodeTest Test { get; }

    /// <summary>Gets the predicates (productions PredicateList and Predicate).</summary>
    public IReadOnlyList<Expression> Predicates { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(Test);
        children.AddRange(Predicates);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAxisStep(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAxisStep(this);
}
