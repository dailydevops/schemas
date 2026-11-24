namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>A parenthesized item type, <c>(type)</c> (production ParenthesizedItemType, XPath 3.0).</summary>
internal sealed class ParenthesizedItemType : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="ParenthesizedItemType"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="inner">Gets the inner.</param>
    public ParenthesizedItemType(SourceSpan location, ItemType inner)
        : base(location) => Inner = inner;

    /// <summary>Gets the inner.</summary>
    public ItemType Inner { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [Inner];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitParenthesizedItemType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitParenthesizedItemType(this);
}
