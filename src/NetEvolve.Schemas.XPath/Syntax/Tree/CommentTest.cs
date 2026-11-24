namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>comment()</c> (production CommentTest).</summary>
internal sealed class CommentTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="CommentTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public CommentTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitCommentTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitCommentTest(this);
}
