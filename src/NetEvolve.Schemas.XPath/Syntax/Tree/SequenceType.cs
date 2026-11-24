namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Sequence type (production SeqType); the item type is <see langword="null"/> for <c>empty-sequence()</c>.</summary>
internal sealed class SequenceType : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="SequenceType"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="itemType">Gets the item type, or <see langword="null"/> for <c>empty-sequence()</c>.</param>
    /// <param name="occurrence">Gets the occurrence.</param>
    public SequenceType(SourceSpan location, ItemType? itemType, Occurrence occurrence)
        : base(location)
    {
        ItemType = itemType;
        Occurrence = occurrence;
    }

    /// <summary>Gets the item type, or <see langword="null"/> for <c>empty-sequence()</c>.</summary>
    public ItemType? ItemType { get; }

    /// <summary>Gets the occurrence.</summary>
    public Occurrence Occurrence { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        if (ItemType is not null)
        {
            children.Add(ItemType);
        }
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSequenceType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitSequenceType(this);
}
