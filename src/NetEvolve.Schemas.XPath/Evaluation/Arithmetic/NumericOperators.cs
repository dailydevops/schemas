namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the entries of the numeric types in the operator table, every pair of integer, decimal,
/// float, and double for the six binary operators, and each of the four types for the two unary operators (XPath 2.0, B.2).
/// </summary>
/// <remarks>
/// The numeric implementations promote the pair themselves, so one implementation serves every pair of an operator. A folder for
/// another type family registers its own entries through its own registration file in the same table.
/// </remarks>
internal static class NumericOperators
{
    private static readonly XdmTypeId[] NumericTypes =
    [
        XdmTypeId.XsInteger,
        XdmTypeId.XsDecimal,
        XdmTypeId.XsFloat,
        XdmTypeId.XsDouble,
    ];

    /// <summary>Registers the numeric entries.</summary>
    /// <param name="table">The table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A numeric entry is already registered.</exception>
    public static void Register(ArithmeticOperatorTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        // The contextual form charges the numeric growth budgets of the evaluation; the plain form uses the default budgets.
        RegisterBinary(
            table,
            ArithmeticOperator.Add,
            NumericArithmetic.Add,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) => NumericArithmetic.Add(a, b, c.Limits)
        );
        RegisterBinary(
            table,
            ArithmeticOperator.Subtract,
            NumericArithmetic.Subtract,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) => NumericArithmetic.Subtract(a, b, c.Limits)
        );
        RegisterBinary(
            table,
            ArithmeticOperator.Multiply,
            NumericArithmetic.Multiply,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) => NumericArithmetic.Multiply(a, b, c.Limits)
        );
        RegisterBinary(
            table,
            ArithmeticOperator.Divide,
            NumericArithmetic.Divide,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) => NumericArithmetic.Divide(a, b, c.Limits)
        );
        RegisterBinary(
            table,
            ArithmeticOperator.IntegerDivide,
            NumericArithmetic.IntegerDivide,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) =>
                NumericArithmetic.IntegerDivide(a, b, c.Limits)
        );
        RegisterBinary(
            table,
            ArithmeticOperator.Modulo,
            NumericArithmetic.Modulo,
            static (in XdmItem a, in XdmItem b, XPathEvaluationContext c) => NumericArithmetic.Modulo(a, b, c.Limits)
        );
        RegisterUnary(table, UnaryOperator.Plus, NumericArithmetic.Plus);
        RegisterUnary(table, UnaryOperator.Minus, NumericArithmetic.Minus);
    }

    private static void RegisterBinary(
        ArithmeticOperatorTable table,
        ArithmeticOperator @operator,
        BinaryOperatorImplementation plain,
        ContextBinaryOperatorImplementation contextual
    )
    {
        for (var left = 0; left < NumericTypes.Length; left++)
        {
            for (var right = 0; right < NumericTypes.Length; right++)
            {
                table.Register(@operator, NumericTypes[left], NumericTypes[right], plain, contextual);
            }
        }
    }

    private static void RegisterUnary(
        ArithmeticOperatorTable table,
        UnaryOperator @operator,
        UnaryOperatorImplementation implementation
    )
    {
        for (var type = 0; type < NumericTypes.Length; type++)
        {
            table.Register(@operator, NumericTypes[type], implementation);
        }
    }
}
