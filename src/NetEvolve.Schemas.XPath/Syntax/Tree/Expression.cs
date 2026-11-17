namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Base of every expression form.</summary>
internal abstract class Expression : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="Expression"/> class.</summary>
    /// <param name="location">The source location.</param>
    private protected Expression(SourceSpan location)
        : base(location) { }
}
