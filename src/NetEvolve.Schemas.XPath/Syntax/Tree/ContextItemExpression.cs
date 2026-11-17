namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Context item expression <c>.</c> (production ContextItemExpr).</summary>
internal sealed class ContextItemExpression : Expression
{
    /// <summary>Initializes a new instance of the <see cref="ContextItemExpression"/> class.</summary>
    /// <param name="location">The source location.</param>
    public ContextItemExpression(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitContextItemExpression(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitContextItemExpression(this);
}
