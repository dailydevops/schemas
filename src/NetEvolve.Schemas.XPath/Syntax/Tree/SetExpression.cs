namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Union, intersect, or except expression (productions UnionExpr and IntersectExceptExpr).</summary>
internal sealed class SetExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="SetExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operator">Gets the operator.</param>
    /// <param name="left">Gets the left.</param>
    /// <param name="right">Gets the right.</param>
    public SetExpression(SourceSpan location, SetOperator @operator, Expression left, Expression right)
        : base(location)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    /// <summary>Gets the operator.</summary>
    public SetOperator Operator { get; }

    /// <summary>Gets the left.</summary>
    public Expression Left { get; }

    /// <summary>Gets the right.</summary>
    public Expression Right { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Left, Right];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSetExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSetExpression(this);
}
