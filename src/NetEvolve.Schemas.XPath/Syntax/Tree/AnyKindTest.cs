namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>node()</c> (production AnyKindTest).</summary>
internal sealed class AnyKindTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="AnyKindTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public AnyKindTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitAnyKindTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitAnyKindTest(this);
}
