namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Unary plus or minus, one node per sign (production UnaryExpr).</summary>
internal sealed class UnaryExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="UnaryExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operator">Gets the operator.</param>
    /// <param name="operand">Gets the operand.</param>
    public UnaryExpression(SourceSpan location, UnaryOperator @operator, Expression operand)
        : base(location)
    {
        Operator = @operator;
        Operand = operand;
    }

    /// <summary>Gets the operator.</summary>
    public UnaryOperator Operator { get; }

    /// <summary>Gets the operand.</summary>
    public Expression Operand { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Operand];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitUnaryExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitUnaryExpression(this);
}
