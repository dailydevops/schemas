namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compatibility;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The registration file of this folder: the lowerings of the binary and unary arithmetic expressions.</summary>
/// <remarks>
/// Both lowerings read the operator table at evaluation, so the entries that other folders add to the same table, before or after
/// this registration, are found. The composition root (XP-19) creates one table and passes it to every folder that registers
/// operators; no such root exists yet. When XPath 1.0 compatibility mode is on, the lowerings create the nodes of the
/// <c>Compatibility</c> folder, which convert each operand by <c>fn:number</c> (XPath 2.0, 3.4), instead of
/// <see cref="ArithmeticNode"/> and <see cref="UnaryArithmeticNode"/>.
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
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(table);

        registry.Register<ArithmeticExpression>(
            (node, context) =>
            {
                var left = context.Lower(node.Left);
                var right = context.Lower(node.Right);
                return context.StaticContext.XPath10Compatibility
                    ? new CompatibilityArithmeticNode(node.Operator, left, right, table)
                    : new ArithmeticNode(node.Operator, left, right, table);
            }
        );
        registry.Register<UnaryExpression>(
            (node, context) =>
            {
                var operand = context.Lower(node.Operand);
                return context.StaticContext.XPath10Compatibility
                    ? new CompatibilityUnaryNode(node.Operator, operand, table)
                    : new UnaryArithmeticNode(node.Operator, operand, table);
            }
        );
    }
}
