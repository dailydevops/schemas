namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Base of every XPath 2.0 syntax tree node: immutable, with the source location it was built from.</summary>
internal abstract class SyntaxNode
{
    private protected SyntaxNode(SourceSpan location) => Location = location;

    /// <summary>Gets the offset and length of the node in the expression text.</summary>
    public SourceSpan Location { get; }

    /// <summary>Gets the direct child nodes in source order.</summary>
    public abstract IReadOnlyList<SyntaxNode> Children { get; }

    /// <summary>Dispatches to the visitor method of this node kind.</summary>
    /// <param name="visitor">The visitor.</param>
    public abstract void Accept(SyntaxVisitor visitor);

    /// <summary>Dispatches to the visitor method of this node kind and returns its result.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="visitor">The visitor.</param>
    /// <returns>The result of the visitor method.</returns>
    public abstract TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor);
}
