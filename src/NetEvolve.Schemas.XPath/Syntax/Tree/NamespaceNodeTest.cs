namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>namespace-node()</c> (production NamespaceNodeTest, XPath 3.0).</summary>
internal sealed class NamespaceNodeTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="NamespaceNodeTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    public NamespaceNodeTest(SourceSpan location)
        : base(location) { }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren() => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitNamespaceNodeTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitNamespaceNodeTest(this);
}
