namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Base of item types (production ItemType).</summary>
internal abstract class ItemType : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="ItemType"/> class.</summary>
    /// <param name="location">The source location.</param>
    private protected ItemType(SourceSpan location)
        : base(location) { }
}
