namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Instance of expression (production InstanceofExpr).</summary>
internal sealed class InstanceOfExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="InstanceOfExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand.</param>
    /// <param name="type">Gets the type.</param>
    public InstanceOfExpression(SourceSpan location, Expression operand, SequenceType type)
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
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitInstanceOfExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitInstanceOfExpression(this);
}
