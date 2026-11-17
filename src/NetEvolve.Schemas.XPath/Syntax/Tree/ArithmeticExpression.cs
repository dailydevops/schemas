namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Additive or multiplicative expression (productions AdditiveExpr and MultiplicativeExpr).</summary>
internal sealed class ArithmeticExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ArithmeticExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operator">Gets the operator.</param>
    /// <param name="left">Gets the left.</param>
    /// <param name="right">Gets the right.</param>
    public ArithmeticExpression(SourceSpan location, ArithmeticOperator @operator, Expression left, Expression right)
        : base(location)
    {
        Operator = @operator;
        Left = left;
        Right = right;
    }

    /// <summary>Gets the operator.</summary>
    public ArithmeticOperator Operator { get; }

    /// <summary>Gets the left.</summary>
    public Expression Left { get; }

    /// <summary>Gets the right.</summary>
    public Expression Right { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Left, Right];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArithmeticExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitArithmeticExpression(this);
}
