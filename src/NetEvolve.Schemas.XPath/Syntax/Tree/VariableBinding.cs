namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>One <c>$name in expr</c> clause (productions SimpleForClause and QuantifiedExpr).</summary>
internal sealed class VariableBinding : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="VariableBinding"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    /// <param name="source">Gets the source.</param>
    public VariableBinding(SourceSpan location, QualifiedName name, Expression source)
        : base(location)
    {
        Name = name;
        Source = source;
    }

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <summary>Gets the source.</summary>
    public Expression Source { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Name, Source];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitVariableBinding(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitVariableBinding(this);
}
