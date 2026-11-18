namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The registration file of this folder: the lowerings of the binary and unary arithmetic expressions.</summary>
/// <remarks>
/// Both lowerings read the operator table at evaluation, so the entries that other folders add to the same table, before or after
/// this registration, are found. The composition root (XP-19) creates one table and passes it to every folder that registers
/// operators; no such root exists yet.
/// </remarks>
internal static class ArithmeticLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="table">The operator table that the lowered nodes dispatch through.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry, ArithmeticOperatorTable table)
    {
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(table);

        registry.Register<ArithmeticExpression>(
            (node, context) =>
                new ArithmeticNode(node.Operator, context.Lower(node.Left), context.Lower(node.Right), table)
        );
        registry.Register<UnaryExpression>(
            (node, context) => new UnaryArithmeticNode(node.Operator, context.Lower(node.Operand), table)
        );
    }
}
