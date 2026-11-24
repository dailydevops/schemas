namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Base of every XPath 2.0 and 3.0 syntax tree node: immutable, with the source location it was built from.</summary>
internal abstract class SyntaxNode
{
    private IReadOnlyList<SyntaxNode>? _children;

    private protected SyntaxNode(SourceSpan location) => Location = location;

    /// <summary>Gets the offset and length of the node in the expression text.</summary>
    public SourceSpan Location { get; }

    /// <summary>Gets the direct child nodes in source order; the list is built once and reused on every access.</summary>
    public IReadOnlyList<SyntaxNode> Children => _children ??= CreateChildren();

    /// <summary>Builds the direct child nodes in source order; called at most once per access race, the result is cached.</summary>
    /// <returns>The child nodes.</returns>
    private protected abstract IReadOnlyList<SyntaxNode> CreateChildren();

    /// <summary>Dispatches to the visitor method of this node kind.</summary>
    /// <param name="visitor">The visitor.</param>
    public abstract void Accept(SyntaxVisitor visitor);

    /// <summary>Dispatches to the visitor method of this node kind and returns its result.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="visitor">The visitor.</param>
    /// <returns>The result of the visitor method.</returns>
    public abstract TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
