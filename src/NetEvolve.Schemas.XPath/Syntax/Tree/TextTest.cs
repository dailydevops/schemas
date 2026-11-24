namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>text()</c> (production TextTest).</summary>
internal sealed class TextTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="TextTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public TextTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitTextTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitTextTest(this);
}
