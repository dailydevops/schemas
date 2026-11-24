namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Atomic type (production AtomicType).</summary>
internal sealed class AtomicType : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="AtomicType"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the name.</param>
    public AtomicType(SourceSpan location, QualifiedName name)
        : base(location) => Name = name;

    /// <summary>Gets the name.</summary>
    public QualifiedName Name { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Name];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAtomicType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAtomicType(this);
}
