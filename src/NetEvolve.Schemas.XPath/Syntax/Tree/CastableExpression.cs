namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Castable as expression (production CastableExpr).</summary>
internal sealed class CastableExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="CastableExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand.</param>
    /// <param name="type">Gets the type.</param>
    public CastableExpression(SourceSpan location, Expression operand, SingleType type)
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
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCastableExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitCastableExpression(this);
}
