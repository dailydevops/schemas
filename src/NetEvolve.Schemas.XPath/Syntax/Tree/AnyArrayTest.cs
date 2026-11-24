namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>array(*)</c> (production AnyArrayTest, XPath 3.1).</summary>
internal sealed class AnyArrayTest : ItemType
{
    /// <summary>Initializes a new instance of the <see cref="AnyArrayTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public AnyArrayTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAnyArrayTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAnyArrayTest(this);
}
