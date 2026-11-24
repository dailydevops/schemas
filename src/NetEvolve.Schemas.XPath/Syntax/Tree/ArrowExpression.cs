namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Arrow expression, <c>operand =&gt; target(arguments)</c> (production ArrowExpr, XPath 3.1).</summary>
internal sealed class ArrowExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ArrowExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="operand">Gets the operand.</param>
    /// <param name="target">Gets the target: a <see cref="QualifiedName"/>, a <see cref="VariableReference"/>, or a <see cref="ParenthesizedExpression"/>.</param>
    /// <param name="arguments">Gets the arguments after the operand.</param>
    public ArrowExpression(
        SourceSpan location,
        Expression operand,
        SyntaxNode target,
        IReadOnlyList<Expression> arguments
    )
        : base(location)
    {
        Operand = operand;
        Target = target;
        Arguments = NodeList.Copy(arguments);
    }

    /// <summary>Gets the operand.</summary>
    public Expression Operand { get; }

    /// <summary>Gets the target: a <see cref="QualifiedName"/>, a <see cref="VariableReference"/>, or a <see cref="ParenthesizedExpression"/>.</summary>
    public SyntaxNode Target { get; }

    /// <summary>Gets the arguments after the operand.</summary>
    public IReadOnlyList<Expression> Arguments { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.Add(Operand);
        children.Add(Target);
        children.AddRange(Arguments);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitArrowExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitArrowExpression(this);
}
