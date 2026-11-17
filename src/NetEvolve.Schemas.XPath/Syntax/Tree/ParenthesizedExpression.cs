namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Parenthesized expression (production ParenthesizedExpr); the inner expression is <see langword="null"/> for <c>()</c>.</summary>
internal sealed class ParenthesizedExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ParenthesizedExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="inner">Gets the inner expression, or <see langword="null"/> for the empty sequence <c>()</c>.</param>
    public ParenthesizedExpression(SourceSpan location, Expression? inner)
        : base(location) => Inner = inner;

    /// <summary>Gets the inner expression, or <see langword="null"/> for the empty sequence <c>()</c>.</summary>
    public Expression? Inner { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            if (Inner is not null)
            {
                children.Add(Inner);
            }
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitParenthesizedExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitParenthesizedExpression(this);
}
