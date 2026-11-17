namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Single type (production SingleType): an atomic type with an optional <c>?</c>.</summary>
internal sealed class SingleType : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="SingleType"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    /// <param name="allowsEmpty">Gets a value indicating whether the optional <c>?</c> is present.</param>
    public SingleType(SourceSpan location, QualifiedName name, bool allowsEmpty)
        : base(location)
    {
        Name = name;
        AllowsEmpty = allowsEmpty;
    }

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <summary>Gets a value indicating whether the optional <c>?</c> is present.</summary>
    public bool AllowsEmpty { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSingleType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSingleType(this);
}
