namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using System;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The arithmetic of the numeric types (XQuery 1.0 and XPath 2.0 Functions and Operators, 6.2) with the type promotion of XPath 2.0,
/// B.1: an operand pair is promoted to the first of integer, decimal, float, and double that holds both, and the operation runs in
/// that type. Integers and decimals are exact; float and double follow IEEE 754.
/// </summary>
/// <remarks>
/// <para>
/// An integer is exact at any width: it runs on 64 bits and continues on <see cref="BigInteger"/> when a result does not fit, and
/// the result is an <c>xs:integer</c> item either way (<see cref="XdmItem.FromBigInteger"/>). A <c>div</c> of two integers is a
/// decimal, as the specification defines.
/// </para>
/// <para>
/// Limitations: a decimal <c>div</c> is rounded half to even at 18 fraction digits, because the precision is implementation-defined
/// (#100 AP-7); the decimal operations align scales with a power of ten, so their cost grows with the scale difference, and an integer beyond 64 bits grows, so a product or sum of large
/// operands costs memory and time in proportion to its width; both are bounded by the evaluation limits <see cref="XPathLimits.MaxDecimalScale"/> and <see cref="XPathLimits.MaxIntegerBits"/>
/// (#639), which end in the coded diagnostics XP0009 and XP0008 and are charged by the overloads that take the limits; the overloads
/// without them use <see cref="XPathLimits.Default"/>. A decimal mantissa is not charged, only its scale; a decimal beyond the scale range of the Xml package
/// raises FOAR0002. <c>idiv</c> of float
/// and double is the truncated quotient of the division, as the specification equates it with <c>($a div $b) cast as xs:integer</c>,
/// so it can differ from the exact quotient by the rounding of the division. Steady state allocates nothing for the 64-bit
/// integer, float, and double paths; decimals and integers beyond 64 bits allocate, as the data model documents.
/// </para>
/// </remarks>
internal static class NumericArithmetic
{
    // The arithmetic has no source location at run time, as the error model defines for dynamic errors.
    private static readonly SourceSpan NoLocation = new(-1, 0);

    // The order is the promotion order of B.1: integer, then decimal, then float, then double.
    private enum Rank
    {
        Integer = 0,
        Decimal = 1,
        Float = 2,
        Double = 3,
    }

    /// <summary>Adds two numbers (<c>op:numeric-add</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The sum, in the promoted type.</returns>
    /// <exception cref="XPathException">The result overflows (FOAR0002).</exception>
    public static XdmItem Add(in XdmItem left, in XdmItem right) => Add(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-add</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem Add(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.Add, left, right, limits);

    /// <summary>Subtracts two numbers (<c>op:numeric-subtract</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The difference, in the promoted type.</returns>
    /// <exception cref="XPathException">The result overflows (FOAR0002).</exception>
    public static XdmItem Subtract(in XdmItem left, in XdmItem right) => Subtract(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-subtract</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem Subtract(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.Subtract, left, right, limits);

    /// <summary>Multiplies two numbers (<c>op:numeric-multiply</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The product, in the promoted type.</returns>
    /// <exception cref="XPathException">The result overflows (FOAR0002).</exception>
    public static XdmItem Multiply(in XdmItem left, in XdmItem right) => Multiply(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-multiply</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem Multiply(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.Multiply, left, right, limits);

    /// <summary>Divides two numbers (<c>op:numeric-divide</c>).</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The quotient: a decimal for two integers or decimals, otherwise in the promoted type.</returns>
    /// <exception cref="XPathException">An integer or decimal divisor is zero (FOAR0001).</exception>
    public static XdmItem Divide(in XdmItem left, in XdmItem right) => Divide(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-divide</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem Divide(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.Divide, left, right, limits);

    /// <summary>Divides two numbers and truncates toward zero (<c>op:numeric-integer-divide</c>).</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The integer quotient, an <c>xs:integer</c> for every operand type.</returns>
    /// <exception cref="XPathException">The divisor is zero (FOAR0001), or the dividend is NaN or infinite, or the divisor is NaN (FOAR0002).</exception>
    public static XdmItem IntegerDivide(in XdmItem left, in XdmItem right) =>
        IntegerDivide(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-integerdivide</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem IntegerDivide(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.IntegerDivide, left, right, limits);

    /// <summary>Gets the remainder of a truncating division, with the sign of the dividend (<c>op:numeric-mod</c>).</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The remainder, in the promoted type.</returns>
    /// <exception cref="XPathException">An integer or decimal divisor is zero (FOAR0001).</exception>
    public static XdmItem Modulo(in XdmItem left, in XdmItem right) => Modulo(left, right, XPathLimits.Default);

    /// <summary>Applies <c>op:numeric-modulo</c> under the numeric growth budgets of <paramref name="limits"/>.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="limits">The budgets.</param>
    /// <returns>The result, in the promoted type.</returns>
    /// <exception cref="XPathException">The operation fails as in the overload without limits, or an integer beyond 64 bits is wider, or a decimal has a larger scale, than the budget allows (XP0008, XP0009).</exception>
    public static XdmItem Modulo(in XdmItem left, in XdmItem right, XPathLimits limits) =>
        Apply(ArithmeticOperator.Modulo, left, right, limits);

    /// <summary>Returns the operand (<c>op:numeric-unary-plus</c>).</summary>
    /// <param name="operand">The operand.</param>
    /// <returns>The operand.</returns>
    public static XdmItem Plus(in XdmItem operand) => operand;

    /// <summary>Negates the operand (<c>op:numeric-unary-minus</c>).</summary>
    /// <param name="operand">The operand.</param>
    /// <returns>The negated operand, in its own type; a negative zero for a positive floating-point zero.</returns>
    public static XdmItem Minus(in XdmItem operand)
    {
        switch (RankOf(operand))
        {
            case Rank.Integer:
                return operand.TryGetInteger(out var integer) && integer != long.MinValue
                    ? XdmItem.FromInteger(-integer)
                    : XdmItem.FromBigInteger(-ToBigInteger(operand));
            case Rank.Decimal:
                return XdmItem.FromDecimal(ToDecimal(operand).Negate());
            case Rank.Float:
                return XdmItem.FromFloat(-ToSingle(operand));
            default:
                return XdmItem.FromDouble(-ToDouble(operand));
        }
    }

    private static XdmItem Apply(ArithmeticOperator op, in XdmItem left, in XdmItem right, XPathLimits limits)
    {
        var rank = (Rank)Math.Max((int)RankOf(left), (int)RankOf(right));
        switch (rank)
        {
            case Rank.Integer when op != ArithmeticOperator.Divide:
                return ApplyInteger(op, left, right, limits);
            case Rank.Integer:
            case Rank.Decimal:
                return ApplyDecimal(op, ToDecimal(left), ToDecimal(right), limits);
            case Rank.Float:
                return ApplySingle(op, ToSingle(left), ToSingle(right));
            default:
                return ApplyDouble(op, ToDouble(left), ToDouble(right));
        }
    }

    // Integers: 64 bits while the result fits, BigInteger beyond.
    private static XdmItem ApplyInteger(ArithmeticOperator op, in XdmItem left, in XdmItem right, XPathLimits limits)
    {
        var narrow = left.TryGetInteger(out var a) & right.TryGetInteger(out var b);
        switch (op)
        {
            case ArithmeticOperator.Add:
                if (narrow)
                {
                    var sum = unchecked(a + b);
                    if (((a ^ sum) & (b ^ sum)) >= 0)
                    {
                        return XdmItem.FromInteger(sum);
                    }
                }

                return Wide(limits, ToBigInteger(left) + ToBigInteger(right));
            case ArithmeticOperator.Subtract:
                if (narrow)
                {
                    var difference = unchecked(a - b);
                    if (((a ^ b) & (a ^ difference)) >= 0)
                    {
                        return XdmItem.FromInteger(difference);
                    }
                }

                return Wide(limits, ToBigInteger(left) - ToBigInteger(right));
            case ArithmeticOperator.Multiply:
                if (narrow && TryMultiply(a, b, out var product))
                {
                    return XdmItem.FromInteger(product);
                }

                var x = ToBigInteger(left);
                var y = ToBigInteger(right);
                limits.ThrowIfProductTooWide(x, y, NoLocation);
                return Wide(limits, x * y);
            case ArithmeticOperator.IntegerDivide:
                if (narrow)
                {
                    // long.MinValue idiv -1 is the one quotient beyond 64 bits.
                    if (b == 0)
                    {
                        throw Zero();
                    }

                    return b != -1 ? XdmItem.FromInteger(a / b)
                        : a != long.MinValue ? XdmItem.FromInteger(-a)
                        : XdmItem.FromBigInteger(-new BigInteger(a));
                }

                return XdmItem.FromBigInteger(BigInteger.Divide(ToBigInteger(left), NonZero(ToBigInteger(right))));
            default:
                if (narrow)
                {
                    return b == 0 ? throw Zero()
                        : b == -1 ? XdmItem.FromInteger(0)
                        : XdmItem.FromInteger(a % b);
                }

                return XdmItem.FromBigInteger(BigInteger.Remainder(ToBigInteger(left), NonZero(ToBigInteger(right))));
        }
    }

    private static XdmItem ApplyDecimal(ArithmeticOperator op, in XsdDecimal a, in XsdDecimal b, XPathLimits limits)
    {
        limits.ThrowIfScaleTooLarge(a.Scale, NoLocation);
        limits.ThrowIfScaleTooLarge(b.Scale, NoLocation);
        try
        {
            switch (op)
            {
                case ArithmeticOperator.Add:
                    return XdmItem.FromDecimal(a.Add(b));
                case ArithmeticOperator.Subtract:
                    return XdmItem.FromDecimal(a.Subtract(b));
                case ArithmeticOperator.Multiply:
                    var product = a.Multiply(b);
                    limits.ThrowIfScaleTooLarge(product.Scale, NoLocation);
                    return XdmItem.FromDecimal(product);
                case ArithmeticOperator.Divide:
                    return XdmItem.FromDecimal(Unwrap(DecimalArithmetic.Divide(a, b)));
                case ArithmeticOperator.IntegerDivide:
                    return XdmItem.FromBigInteger(Unwrap(DecimalArithmetic.IntegerDivide(a, b)));
                default:
                    return XdmItem.FromDecimal(Unwrap(DecimalArithmetic.Remainder(a, b)));
            }
        }
        catch (OverflowException)
        {
            // XsdDecimal reports a scale beyond the 32-bit range with an OverflowException.
            throw XPathErrors.Exception(XPathErrorCodes.FOAR0002);
        }
    }

    private static XdmItem ApplySingle(ArithmeticOperator op, float a, float b)
    {
        switch (op)
        {
            case ArithmeticOperator.Add:
                return XdmItem.FromFloat(a + b);
            case ArithmeticOperator.Subtract:
                return XdmItem.FromFloat(a - b);
            case ArithmeticOperator.Multiply:
                return XdmItem.FromFloat(a * b);
            case ArithmeticOperator.Divide:
                return XdmItem.FromFloat(a / b);
            case ArithmeticOperator.IntegerDivide:
                if (b == 0)
                {
                    throw Zero();
                }

                var quotient = a / b;
                return float.IsNaN(quotient) || float.IsInfinity(quotient) ? throw Overflow() : Truncate(quotient);
            default:
                return XdmItem.FromFloat(a % b);
        }
    }

    private static XdmItem ApplyDouble(ArithmeticOperator op, double a, double b)
    {
        switch (op)
        {
            case ArithmeticOperator.Add:
                return XdmItem.FromDouble(a + b);
            case ArithmeticOperator.Subtract:
                return XdmItem.FromDouble(a - b);
            case ArithmeticOperator.Multiply:
                return XdmItem.FromDouble(a * b);
            case ArithmeticOperator.Divide:
                return XdmItem.FromDouble(a / b);
            case ArithmeticOperator.IntegerDivide:
                if (b == 0)
                {
                    throw Zero();
                }

                var quotient = a / b;
                return double.IsNaN(quotient) || double.IsInfinity(quotient) ? throw Overflow() : Truncate(quotient);
            default:
                return XdmItem.FromDouble(a % b);
        }
    }

    // The integer part of a finite quotient; at 2^63 and beyond every double is an integer, so the conversion is exact.
    private static XdmItem Truncate(double quotient)
    {
        if (quotient > -9223372036854775808.0 && quotient < 9223372036854775808.0)
        {
            return XdmItem.FromInteger((long)quotient);
        }

        var exact = DecimalArithmetic.FromDouble(quotient).Value;
        return XdmItem.FromBigInteger(DecimalArithmetic.ToBigInteger(exact).Value);
    }

    // The overflow of the 64-bit product, detected without an exception or a wider type.
    private static bool TryMultiply(long a, long b, out long product)
    {
        product = 0;
        if (a == 0 || b == 0)
        {
            return true;
        }

        if ((a == -1 && b == long.MinValue) || (b == -1 && a == long.MinValue))
        {
            return false;
        }

        var result = unchecked(a * b);
        if (result / b != a)
        {
            return false;
        }

        product = result;
        return true;
    }

    private static Rank RankOf(in XdmItem item)
    {
        switch (item.Kind)
        {
            case XdmItemKind.XsInteger:
                return Rank.Integer;
            case XdmItemKind.XsDecimal:
                // An integer beyond 64 bits is a decimal item that carries the xs:integer type.
                return XdmTypes.IsDerivedFrom(item.TypeId, XdmTypeId.XsInteger) ? Rank.Integer : Rank.Decimal;
            case XdmItemKind.XsFloat:
                return Rank.Float;
            case XdmItemKind.XsDouble:
                return Rank.Double;
            default:
                // The operator table maps numeric types only, so this is a registration error.
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }
    }

    // An integer result beyond 64 bits is charged against the width budget; a result that fits 64 bits is never charged.
    private static XdmItem Wide(XPathLimits limits, BigInteger value)
    {
        limits.ThrowIfIntegerTooWide(value, NoLocation);
        return XdmItem.FromBigInteger(value);
    }

    private static BigInteger ToBigInteger(in XdmItem item) => item.TryGetBigInteger(out var value) ? value : default;

    private static XsdDecimal ToDecimal(in XdmItem item) =>
        item.TryGetDecimal(out var value)
            ? value
            : new XsdDecimal(item.TryGetInteger(out var integer) ? integer : 0, 0);

    // The promotion of B.1 lives in NumericPromotion, shared with the function conversion; the operator table maps numeric types only.
    private static float ToSingle(in XdmItem item) =>
        NumericPromotion.TryToSingle(item, out var value)
            ? value
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    private static double ToDouble(in XdmItem item) =>
        NumericPromotion.TryToDouble(item, out var value)
            ? value
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    private static TValue Unwrap<TValue>(ArithmeticResult<TValue> result) =>
        result.Success ? result.Value : throw (result.Error == DatatypeError.DivisionByZero ? Zero() : Overflow());

    private static BigInteger NonZero(BigInteger value) => value.IsZero ? throw Zero() : value;

    private static XPathException Zero() => XPathErrors.Exception(XPathErrorCodes.FOAR0001);

    private static XPathException Overflow() => XPathErrors.Exception(XPathErrorCodes.FOAR0002);
}
