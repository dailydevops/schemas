namespace NetEvolve.Schemas.XPath.Evaluation.Arrow;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The registration file of this folder: the lowering of the arrow operator <c>E =&gt; f(args)</c> (XPath 3.1, 3.16), which is the call
/// <c>f(E, args)</c> with the left operand as the first argument.
/// </summary>
/// <remarks>
/// A name as the target lowers to a static call and a variable or parenthesized expression to a dynamic call, so a placeholder in the
/// argument list, the arity check, and the error positions come from those lowerings; the synthesized call spans the arrow expression.
/// The operand is passed as a whole sequence, never item by item.
/// </remarks>
internal static class ArrowLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<ArrowExpression>(static (node, context) => context.Lower(ToCall(node)));
    }

    private static Expression ToCall(ArrowExpression node)
    {
        var arguments = new List<Expression>(node.Arguments.Count + 1) { node.Operand };
        arguments.AddRange(node.Arguments);
        return node.Target is QualifiedName name
            ? new FunctionCall(node.Location, name, arguments)
            : new DynamicFunctionCall(node.Location, (Expression)node.Target, arguments);
    }
}
