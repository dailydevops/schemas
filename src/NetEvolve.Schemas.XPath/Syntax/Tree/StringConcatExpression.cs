namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>String concatenation, <c>left || right</c> (production StringConcatExpr, XPath 3.0).</summary>
internal sealed class StringConcatExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="StringConcatExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="left">Gets the left.</param>
    /// <param name="right">Gets the right.</param>
    public StringConcatExpression(SourceSpan location, Expression left, Expression right)
        : base(location)
    {
        Left = left;
        Right = right;
    }

    /// <summary>Gets the left.</summary>
    public Expression Left { get; }

    /// <summary>Gets the right.</summary>
    public Expression Right { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Left, Right];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitStringConcatExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitStringConcatExpression(this);
}
