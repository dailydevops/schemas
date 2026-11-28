namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

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

        RegisterBinary(table, ArithmeticOperator.Add, NumericArithmetic.Add);
        RegisterBinary(table, ArithmeticOperator.Subtract, NumericArithmetic.Subtract);
        RegisterBinary(table, ArithmeticOperator.Multiply, NumericArithmetic.Multiply);
        RegisterBinary(table, ArithmeticOperator.Divide, NumericArithmetic.Divide);
        RegisterBinary(table, ArithmeticOperator.IntegerDivide, NumericArithmetic.IntegerDivide);
        RegisterBinary(table, ArithmeticOperator.Modulo, NumericArithmetic.Modulo);
        RegisterUnary(table, UnaryOperator.Plus, NumericArithmetic.Plus);
        RegisterUnary(table, UnaryOperator.Minus, NumericArithmetic.Minus);
    }

    private static void RegisterBinary(
        ArithmeticOperatorTable table,
        ArithmeticOperator @operator,
        BinaryOperatorImplementation implementation
    )
    {
        for (var left = 0; left < NumericTypes.Length; left++)
        {
            for (var right = 0; right < NumericTypes.Length; right++)
            {
                table.Register(@operator, NumericTypes[left], NumericTypes[right], implementation);
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
