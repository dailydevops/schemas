namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>item()</c>.</summary>
internal sealed class AnyItemType : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="AnyItemType"/> class.</summary>
    /// <param name="location">The source location.</param>
    public AnyItemType(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAnyItemType(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAnyItemType(this);
}
