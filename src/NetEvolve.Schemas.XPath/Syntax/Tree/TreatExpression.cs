namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Treat as expression (production TreatExpr).</summary>
internal sealed class TreatExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="TreatExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand.</param>
    /// <param name="type">Gets the type.</param>
    public TreatExpression(SourceSpan location, Expression operand, SequenceType type)
        : base(location)
    {
        Operand = operand;
        Type = type;
    }

    /// <summary>Gets the operand.</summary>
    public Expression Operand { get; }

    /// <summary>Gets the type.</summary>
    public SequenceType Type { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Operand, Type];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTreatExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitTreatExpression(this);
}
