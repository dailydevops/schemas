namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ArithmeticFixture;

public class NumericArithmeticTests
{
    private static readonly string[] Types = ["i", "d", "f", "e"];
    private static readonly string[] Operators = ["+", "-", "*", "div", "idiv", "mod"];

    // The operands are 6 and 4 in every type pair; the results are 10, 2, 24, 1.5, 1, and 2.
    private static readonly Dictionary<string, string> Results = new(StringComparer.Ordinal)
    {
        ["+"] = "10",
        ["-"] = "2",
        ["*"] = "24",
        ["div"] = "1.5",
        ["idiv"] = "1",
        ["mod"] = "2",
    };

    public static IEnumerable<Func<(string Left, string Operator, string Right, string Expected)>> TypePairs()
    {
        foreach (var op in Operators)
        {
            foreach (var left in Types)
            {
                foreach (var right in Types)
                {
                    var row = (left + ":6", op, right + ":4", ExpectedOf(op, left, right));
                    yield return () => row;
                }
            }
        }
    }

    // XPath 2.0, B.1 and B.2: float and double win over decimal, decimal over integer, double over float; idiv always gives an
    // integer; div of two integers gives a decimal.
    private static string ExpectedOf(string op, string left, string right)
    {
        var rank = Math.Max(Array.IndexOf(Types, left), Array.IndexOf(Types, right));
        var type =
            string.Equals(op, "idiv", StringComparison.Ordinal) ? "i"
            : string.Equals(op, "div", StringComparison.Ordinal) && rank == 0 ? "d"
            : Types[rank];
        return type + ":" + Results[op];
    }

    [Test]
    [MethodDataSource(nameof(TypePairs))]
    public async Task EveryOperatorAndTypePair_GivesTheValueAndTheTypeOfTheSpecification(
        string left,
        string op,
        string right,
        string expected
    )
    {
        var actual = Run(left, op, right);

        await Assert.That(Describe(actual)).IsEqualTo(Describe(expected));
    }

    [Test]
    public async Task IntegerDivInteger_IsADecimalNotAnInteger()
    {
        await Assert.That(Describe(Run("i:8", "div", "i:2"))).IsEqualTo("decimal:4");
        await Assert.That(Describe(Run("i:7", "div", "i:2"))).IsEqualTo("decimal:3.5");
    }

    [Test]
    public async Task IntegerDivInteger_RoundsToEighteenFractionDigits()
    {
        await Assert.That(Describe(Run("i:1", "div", "i:3"))).IsEqualTo("decimal:0.333333333333333333");
        await Assert.That(Describe(Run("i:2", "div", "i:3"))).IsEqualTo("decimal:0.666666666666666667");
    }

    [Test]
    [Arguments("i:3", "+", "e:0.5", "double:3.5")]
    [Arguments("d:1.25", "*", "e:2", "double:2.5")]
    [Arguments("f:1.5", "+", "e:0.25", "double:1.75")]
    [Arguments("i:3", "*", "f:0.5", "float:1.5")]
    [Arguments("d:0.5", "+", "f:0.25", "float:0.75")]
    public async Task Promotion_PicksTheCommonTypeBeforeTheOperation(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task Promotion_FloatAndDouble_RoundsTheDecimalOnceToTheTargetType()
    {
        // 0.1 is not a float or a double; adding it to 0 shows the value that the promotion produced.
        await Assert.That(Describe(Run("d:0.1", "+", "f:0"))).IsEqualTo("float:0.1");
        await Assert.That(Describe(Run("d:0.1", "+", "e:0"))).IsEqualTo("double:0.1");
    }

    [Test]
    [Arguments("e:1.5", "+", "e:2.25", "double:3.75")]
    [Arguments("e:1.5", "-", "e:2.25", "double:-0.75")]
    [Arguments("e:1.5", "*", "e:2", "double:3")]
    [Arguments("e:1.5", "div", "e:2", "double:0.75")]
    [Arguments("f:1.5", "+", "f:2.25", "float:3.75")]
    [Arguments("f:1.5", "-", "f:2.25", "float:-0.75")]
    [Arguments("f:1.5", "*", "f:2", "float:3")]
    [Arguments("f:1.5", "div", "f:2", "float:0.75")]
    [Arguments("d:1.5", "+", "d:2.25", "decimal:3.75")]
    [Arguments("d:1.5", "-", "d:2.25", "decimal:-0.75")]
    [Arguments("d:1.5", "*", "d:2", "decimal:3")]
    [Arguments("d:3", "div", "d:4", "decimal:0.75")]
    public async Task Operators_PerType_GiveTheValue(string left, string op, string right, string expected) =>
        await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task FloatArithmetic_RoundsToFloat_NotToDouble()
    {
        // 16777216 + 1 is not a float; it rounds back to 16777216, while a double keeps the one.
        await Assert.That(Describe(Run("f:16777216", "+", "f:1"))).IsEqualTo("float:16777216");
        await Assert.That(Describe(Run("e:16777216", "+", "e:1"))).IsEqualTo("double:16777217");
    }

    [Test]
    [Arguments("i:7", "mod", "i:3", "integer:1")]
    [Arguments("i:-7", "mod", "i:3", "integer:-1")]
    [Arguments("i:7", "mod", "i:-3", "integer:1")]
    [Arguments("i:-7", "mod", "i:-3", "integer:-1")]
    [Arguments("i:7", "idiv", "i:3", "integer:2")]
    [Arguments("i:-7", "idiv", "i:3", "integer:-2")]
    [Arguments("i:7", "idiv", "i:-3", "integer:-2")]
    [Arguments("i:-7", "idiv", "i:-3", "integer:2")]
    [Arguments("d:7.5", "mod", "d:2", "decimal:1.5")]
    [Arguments("d:-7.5", "mod", "d:2", "decimal:-1.5")]
    [Arguments("d:7.5", "mod", "d:-2", "decimal:1.5")]
    [Arguments("d:-7.5", "mod", "d:-2", "decimal:-1.5")]
    [Arguments("d:7.5", "idiv", "d:2", "integer:3")]
    [Arguments("d:-7.5", "idiv", "d:2", "integer:-3")]
    [Arguments("d:7.5", "idiv", "d:-2", "integer:-3")]
    [Arguments("d:-7.5", "idiv", "d:-2", "integer:3")]
    [Arguments("e:7.5", "mod", "e:2", "double:1.5")]
    [Arguments("e:-7.5", "mod", "e:2", "double:-1.5")]
    [Arguments("e:7.5", "mod", "e:-2", "double:1.5")]
    [Arguments("e:-7.5", "mod", "e:-2", "double:-1.5")]
    [Arguments("e:7.5", "idiv", "e:2", "integer:3")]
    [Arguments("e:-7.5", "idiv", "e:2", "integer:-3")]
    [Arguments("e:7.5", "idiv", "e:-2", "integer:-3")]
    [Arguments("e:-7.5", "idiv", "e:-2", "integer:3")]
    [Arguments("f:7.5", "idiv", "f:2", "integer:3")]
    [Arguments("f:-7.5", "mod", "f:2", "float:-1.5")]
    public async Task ModAndIdiv_WithNegativeOperands_TruncateTowardZeroAndTakeTheSignOfTheDividend(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task Mod_ResultZero_KeepsTheSignOfTheDividendForFloatingPointOnly()
    {
        await Assert.That(Describe(Run("i:-4", "mod", "i:2"))).IsEqualTo("integer:0");
        await Assert.That(Describe(Run("d:-4", "mod", "d:2"))).IsEqualTo("decimal:0");
        await Assert.That(Describe(Run("e:-4", "mod", "e:2"))).IsEqualTo("double:-0");
        await Assert.That(Describe(Run("e:4", "mod", "e:-2"))).IsEqualTo("double:0");
    }

    [Test]
    [Arguments(
        "d:12345678901234567890123456789.12345678901234567890",
        "+",
        "d:0.00000000000000000010",
        "decimal:12345678901234567890123456789.123456789012345679"
    )]
    [Arguments(
        "d:0.1234567890123456789012345678901",
        "+",
        "d:0.0000000000000000000000000000001",
        "decimal:0.1234567890123456789012345678902"
    )]
    [Arguments(
        "d:0.1234567890123456789012345678901",
        "-",
        "d:0.0000000000000000000000000000001",
        "decimal:0.12345678901234567890123456789"
    )]
    [Arguments(
        "d:1234567890123456789012345678901234567890.5",
        "-",
        "d:1234567890123456789012345678901234567890",
        "decimal:0.5"
    )]
    public async Task Decimal_BeyondTwentyEightDigits_IsExact(string left, string op, string right, string expected) =>
        await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task Decimal_ProductBeyondTwentyEightDigits_IsExact()
    {
        var tiny = "d:0." + new string('0', 30) + "1";
        var expected = "decimal:0." + new string('0', 61) + "1";

        await Assert.That(Describe(Run(tiny, "*", tiny))).IsEqualTo(expected);

        // 12345678901234567890.12345678901234567890 x 98765432109876543210.98765432109876543210 has scale 40.
        var product = (
            BigInteger.Parse("1234567890123456789012345678901234567890", CultureInfo.InvariantCulture)
            * BigInteger.Parse("9876543210987654321098765432109876543210", CultureInfo.InvariantCulture)
        ).ToString(CultureInfo.InvariantCulture);
        var whole = product.Substring(0, product.Length - 40);
        var fraction = product.Substring(product.Length - 40).TrimEnd('0');
        var actual = Run(
            "d:12345678901234567890.12345678901234567890",
            "*",
            "d:98765432109876543210.98765432109876543210"
        );

        await Assert.That(Describe(actual)).IsEqualTo("decimal:" + whole + "." + fraction);
    }

    [Test]
    public async Task Decimal_ProductBeyondTheScaleRange_RaisesFoar0002()
    {
        var smallest = XdmSequence.Create(XdmItem.FromDecimal(new XsdDecimal(BigInteger.One, int.MaxValue)));

        var error = Raised(() => Run(ArithmeticOperator.Multiply, smallest, smallest));

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.FOAR0002);
    }

    [Test]
    [Arguments("w:9223372036854775807", "+", "i:1", "integer:9223372036854775808")]
    [Arguments("i:9223372036854775807", "+", "i:1", "integer:9223372036854775808")]
    [Arguments("i:-9223372036854775808", "-", "i:1", "integer:-9223372036854775809")]
    [Arguments("i:9223372036854775807", "*", "i:9223372036854775807", "integer:85070591730234615847396907784232501249")]
    [Arguments("i:-9223372036854775808", "idiv", "i:-1", "integer:9223372036854775808")]
    [Arguments("w:100000000000000000000", "mod", "i:7", "integer:2")]
    [Arguments("w:100000000000000000000", "idiv", "i:7", "integer:14285714285714285714")]
    [Arguments("w:-100000000000000000000", "mod", "i:7", "integer:-2")]
    [Arguments("w:-100000000000000000000", "idiv", "i:7", "integer:-14285714285714285714")]
    [Arguments("w:100000000000000000000", "div", "i:4", "decimal:25000000000000000000")]
    [Arguments("w:100000000000000000000", "+", "d:0.5", "decimal:100000000000000000000.5")]
    [Arguments("w:100000000000000000000", "+", "e:1", "double:1E+20")]
    [Arguments("w:100000000000000000000", "+", "f:1", "float:1E+20")]
    [Arguments("w:100000000000000000000", "idiv", "w:100000000000000000000", "integer:1")]
    [Arguments("i:9223372036854775807", "-", "i:-1", "integer:9223372036854775808")]
    [Arguments("i:-9223372036854775808", "-", "w:9223372036854775808", "integer:-18446744073709551616")]
    [Arguments("i:-1", "*", "i:-9223372036854775808", "integer:9223372036854775808")]
    [Arguments("i:-9223372036854775808", "*", "i:-1", "integer:9223372036854775808")]
    [Arguments("i:4294967296", "*", "i:2147483647", "integer:9223372032559808512")]
    [Arguments("i:0", "*", "i:-9223372036854775808", "integer:0")]
    [Arguments("i:-9223372036854775808", "mod", "i:-1", "integer:0")]
    [Arguments("i:-9223372036854775808", "idiv", "i:1", "integer:-9223372036854775808")]
    [Arguments("i:9223372036854775807", "idiv", "i:-1", "integer:-9223372036854775807")]
    [Arguments("w:100000000000000000000", "-", "w:99999999999999999999", "integer:1")]
    [Arguments("w:100000000000000000000", "-", "w:100000000000000000000", "integer:0")]
    [Arguments(
        "w:100000000000000000000",
        "*",
        "w:100000000000000000000",
        "integer:10000000000000000000000000000000000000000"
    )]
    public async Task Integer_BeyondSixtyFourBits_IsExactAndStaysAnInteger(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    public async Task Integer_ResultThatFitsSixtyFourBitsAgain_IsTheInlineInteger()
    {
        var result = Run("w:100000000000000000000", "-", "w:99999999999999999999");

        await Assert.That(result[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(1L);
    }

    [Test]
    public async Task IdivOfFloatingPoint_IsTheTruncatedQuotientOfTheDiv_AsTheSpecificationEquates()
    {
        // The double 0.1 is a little above 0.1, so the exact quotient is just below 10, but 1e0 div 0.1e0 rounds to exactly 10.
        await Assert.That(Describe(Run("e:1", "div", "e:0.1"))).IsEqualTo("double:10");
        await Assert.That(Describe(Run("e:1", "idiv", "e:0.1"))).IsEqualTo("integer:10");
        await Assert.That(Describe(Run("d:1", "idiv", "d:0.1"))).IsEqualTo("integer:10");
    }

    [Test]
    public async Task IdivOfFloatingPoint_BeyondSixtyFourBits_IsAnExactInteger()
    {
        await Assert.That(Describe(Run("e:1E20", "idiv", "e:1"))).IsEqualTo("integer:100000000000000000000");
        await Assert.That(Describe(Run("e:-1E20", "idiv", "i:1"))).IsEqualTo("integer:-100000000000000000000");
    }

    [Test]
    public async Task Implementation_CalledWithANonNumericItem_RaisesXpty0004()
    {
        var binary = Raised(() => XdmSequence.Create(NumericArithmetic.Add(Item("s:a"), Item("i:1"))));
        var unary = Raised(() => XdmSequence.Create(NumericArithmetic.Minus(Item("b:true"))));

        await Assert.That(binary!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPTY0004);
        await Assert.That(unary!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPTY0004);
    }
}
