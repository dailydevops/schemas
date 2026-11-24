namespace NetEvolve.Schemas.XPath.Evaluation.Flow30;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The registration file of this folder: the lowerings of the three small XPath 3.0 expression forms, the <see langword="let"/>
/// expression, the simple map operator, and the string concatenation operator.
/// </summary>
/// <remarks>
/// <para>
/// An expression with several bindings is lowered as a chain, like the <see langword="for"/> expression: <c>let $a := A, $b := B return R</c>
/// becomes <c>let $a := A return (let $b := B return R)</c>, so a source sees the earlier variables and not its own.
/// </para>
/// <para>
/// A chain of <c>||</c> operators is flattened into one node with all operands, so the result is built once, without an intermediate
/// string per operator.
/// </para>
/// </remarks>
internal static class Flow30Lowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<LetExpression>(static (node, context) => LowerLet(node, context));
        registry.Register<SimpleMapExpression>(
            static (node, context) =>
                new SimpleMapNode(context.Lower(node.Left), context.Lower(node.Right), node.Location)
        );
        registry.Register<StringConcatExpression>(static (node, context) => LowerConcat(node, context));
    }

    private static LetNode LowerLet(LetExpression node, LoweringContext context)
    {
        var binding = node.Bindings[0];
        var source = context.Lower(binding.Source);
        using var scope = context.DeclareVariable(binding.Name);
        var name = context.ResolveVariable(binding.Name);
        var body =
            node.Bindings.Count == 1
                ? context.Lower(node.Return)
                : context.Lower(new LetExpression(node.Location, Tail(node.Bindings), node.Return));
        return new LetNode(name, source, body);
    }

    private static StringConcatNode LowerConcat(StringConcatExpression node, LoweringContext context)
    {
        // The operator is left-associative: walk down the left spine, then lower the operands in source order.
        var operands = new List<Expression>();
        Expression current = node;
        while (current is StringConcatExpression concat)
        {
            operands.Add(concat.Right);
            current = concat.Left;
        }

        operands.Add(current);
        var lowered = new ExecutableNode[operands.Count];
        for (var i = 0; i < lowered.Length; i++)
        {
            lowered[i] = context.Lower(operands[lowered.Length - 1 - i]);
        }

        return new StringConcatNode(lowered, node.Location);
    }

    private static List<VariableBinding> Tail(IReadOnlyList<VariableBinding> bindings)
    {
        var tail = new List<VariableBinding>(bindings.Count - 1);
        for (var i = 1; i < bindings.Count; i++)
        {
            tail.Add(bindings[i]);
        }

        return tail;
    }
}
