namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Range expression (production RangeExpr).</summary>
internal sealed class RangeExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="RangeExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="from">Gets the from.</param>
    /// <param name="to">Gets the to.</param>
    public RangeExpression(SourceSpan location, Expression from, Expression to)
        : base(location)
    {
        From = from;
        To = to;
    }

    /// <summary>Gets the from.</summary>
    public Expression From { get; }

    /// <summary>Gets the to.</summary>
    public Expression To { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [From, To];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitRangeExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitRangeExpression(this);
}
