namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Variable reference (production VarRef).</summary>
internal sealed class VariableReference : Expression
{
    /// <summary>Initializes a new instance of the <see cref="VariableReference"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    public VariableReference(SourceSpan location, QualifiedName name)
        : base(location) => Name = name;

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitVariableReference(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitVariableReference(this);
}
