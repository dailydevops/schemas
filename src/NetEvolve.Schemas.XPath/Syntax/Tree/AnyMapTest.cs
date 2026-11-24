namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>map(*)</c> (production AnyMapTest, XPath 3.1).</summary>
internal sealed class AnyMapTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="AnyMapTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public AnyMapTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAnyMapTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAnyMapTest(this);
}
