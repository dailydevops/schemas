namespace NetEvolve.Schemas.XPath.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The lowerings by syntax node kind. Each evaluation folder registers the lowerings of its own node kinds; there is no
/// central switch. Not thread-safe for registration: fill it before compiling.
/// </summary>
internal sealed class LoweringRegistry
{
    private readonly Dictionary<Type, Func<SyntaxNode, LoweringContext, ExecutableNode>> _lowerings = [];

    /// <summary>Registers the lowering of a node kind.</summary>
    /// <typeparam name="TNode">The syntax node kind, matched by exact type.</typeparam>
    /// <param name="lowering">The lowering.</param>
    /// <exception cref="InvalidOperationException">The node kind already has a lowering.</exception>
    public void Register<TNode>(Func<TNode, LoweringContext, ExecutableNode> lowering)
        where TNode : SyntaxNode
    {
        ArgumentNullException.ThrowIfNull(lowering);

        if (_lowerings.ContainsKey(typeof(TNode)))
        {
            throw new InvalidOperationException($"A lowering for '{typeof(TNode).Name}' is already registered.");
        }

        _lowerings.Add(typeof(TNode), (node, context) => lowering((TNode)node, context));
    }

    internal bool TryGet(Type nodeType, out Func<SyntaxNode, LoweringContext, ExecutableNode>? lowering) =>
        _lowerings.TryGetValue(nodeType, out lowering);
}
