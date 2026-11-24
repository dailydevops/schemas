namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Cast as expression (production CastExpr).</summary>
internal sealed class CastExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="CastExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand.</param>
    /// <param name="type">Gets the type.</param>
    public CastExpression(SourceSpan location, Expression operand, SingleType type)
        : base(location)
    {
        Operand = operand;
        Type = type;
    }

    /// <summary>Gets the operand.</summary>
    public Expression Operand { get; }

    /// <summary>Gets the type.</summary>
    public SingleType Type { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Operand, Type];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCastExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitCastExpression(this);
}
