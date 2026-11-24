namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>A kind test used as an item type.</summary>
internal sealed class KindItemType : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="KindItemType"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="test">Gets the test.</param>
    public KindItemType(SourceSpan location, KindTest test)
        : base(location) => Test = test;

    /// <summary>Gets the test.</summary>
    public KindTest Test { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Test];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitKindItemType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitKindItemType(this);
}
