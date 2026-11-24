namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Comma expression (production Expr) with two or more operands.</summary>
internal sealed class SequenceExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="SequenceExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="items">Gets the items.</param>
    public SequenceExpression(SourceSpan location, IReadOnlyList<Expression> items)
        : base(location) => Items = NodeList.Copy(items);

    /// <summary>Gets the items.</summary>
    public IReadOnlyList<Expression> Items { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        children.AddRange(Items);
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSequenceExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSequenceExpression(this);
}
