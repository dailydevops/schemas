namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Base of node tests (production NodeTest).</summary>
internal abstract class NodeTest : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="NodeTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    private protected NodeTest(SourceSpan location)
        : base(location) { }
}
