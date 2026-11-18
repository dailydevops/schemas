namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Items written as <c>kind:text</c>: <c>i</c> integer (64 bits), <c>w</c> integer of any width, <c>d</c> decimal, <c>f</c> float,
/// <c>e</c> double, <c>u</c> untypedAtomic, <c>s</c> string, <c>b</c> boolean. Lexical forms go through the datatype parsers of the
/// library, never through the runtime parsers, so every test leg reads the same values.
/// </summary>
internal static class ArithmeticFixture
{
    public static XdmItem Item(string spec)
    {
        var text = spec.Substring(2);
        return spec[0] switch
        {
            'i' => XdmItem.FromInteger(long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)),
            'w' => XdmItem.FromBigInteger(
                BigInteger.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)
            ),
            'd' => XdmItem.FromDecimal(XsdDecimal.Parse(text.AsSpan()).Value),
            'f' => XdmItem.FromFloat(XsdFloatingPoint.ParseSingle(text.AsSpan(), XsdVersion.V10).Value),
            'e' => XdmItem.FromDouble(XsdFloatingPoint.ParseDouble(text.AsSpan(), XsdVersion.V10).Value),
            'u' => XdmItem.FromUntypedAtomic(text),
            's' => XdmItem.FromString(text),
            'b' => XdmItem.FromBoolean(string.Equals(text, "true", StringComparison.Ordinal)),
            _ => throw new InvalidOperationException("Unknown item kind: " + spec),
        };
    }

    public static ArithmeticOperator Operator(string token) =>
        token switch
        {
            "+" => ArithmeticOperator.Add,
            "-" => ArithmeticOperator.Subtract,
            "*" => ArithmeticOperator.Multiply,
            "div" => ArithmeticOperator.Divide,
            "idiv" => ArithmeticOperator.IntegerDivide,
            "mod" => ArithmeticOperator.Modulo,
            _ => throw new InvalidOperationException("Unknown operator: " + token),
        };

    /// <summary>A table with the numeric operators, as the composition root fills it.</summary>
    public static ArithmeticOperatorTable Table()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        return table;
    }

    public static XdmSequence Run(string left, string op, string right) =>
        Run(Operator(op), XdmSequence.Create(Item(left)), XdmSequence.Create(Item(right)));

    public static XdmSequence Run(
        ArithmeticOperator op,
        in XdmSequence left,
        in XdmSequence right,
        ArithmeticOperatorTable? table = null
    )
    {
        var node = new ArithmeticNode(op, new ConstantNode(left), new ConstantNode(right), table ?? Table());
        using var context = new XPathEvaluationContext();
        return node.Evaluate(context);
    }

    public static XdmSequence Run(UnaryOperator op, in XdmSequence operand)
    {
        var node = new UnaryArithmeticNode(op, new ConstantNode(operand), Table());
        using var context = new XPathEvaluationContext();
        return node.Evaluate(context);
    }

    public static XdmSequence Run(UnaryOperator op, string operand) => Run(op, XdmSequence.Create(Item(operand)));

    /// <summary>Runs an action and returns the XPath error it raises, <see langword="null"/> when it raises none.</summary>
    public static XPathException? Raised(Func<XdmSequence> action)
    {
        try
        {
            _ = action();
            return null;
        }
        catch (XPathException exception)
        {
            return exception;
        }
    }

    /// <summary>Describes an item by type and value; a negative zero and the NaN are told apart from their neighbors.</summary>
    public static string Describe(in XdmItem item)
    {
        if (item.TryGetInteger(out var integer))
        {
            return "integer:" + integer.ToString(CultureInfo.InvariantCulture);
        }

        if (item.TryGetDecimal(out var number))
        {
            var kind = item.TypeId == XdmTypeId.XsInteger ? "integer" : "decimal";
            return kind + ":" + number;
        }

        if (item.TryGetDouble(out var wide))
        {
            return "double:" + Describe(wide);
        }

        if (item.TryGetFloat(out var narrow))
        {
            return "float:" + Describe(narrow);
        }

        if (item.TryGetBoolean(out var flag))
        {
            return "boolean:" + flag;
        }

        if (item.TryGetString(out var text))
        {
            return "string:" + text;
        }

        return "kind:" + item.Kind;
    }

    public static string Describe(in XdmSequence sequence)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            _ = builder.Append(i == 0 ? string.Empty : ", ").Append(Describe(sequence[i]));
        }

        return sequence.IsEmpty ? "()" : builder.ToString();
    }

    public static string Describe(string spec) => Describe(Item(spec));

    private static string Describe(double value) =>
        double.IsNaN(value) ? "NaN"
        : double.IsPositiveInfinity(value) ? "INF"
        : double.IsNegativeInfinity(value) ? "-INF"
        : value == 0 ? (BitConverter.DoubleToInt64Bits(value) < 0 ? "-0" : "0")
        : value.ToString("R", CultureInfo.InvariantCulture);

    private static string Describe(float value) =>
        float.IsNaN(value) ? "NaN"
        : float.IsPositiveInfinity(value) ? "INF"
        : float.IsNegativeInfinity(value) ? "-INF"
        : value == 0 ? (BitConverter.SingleToInt32Bits(value) < 0 ? "-0" : "0")
        : value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>An operand that yields a fixed value.</summary>
    public sealed class ConstantNode(XdmSequence value) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => value;
    }

    /// <summary>The lowerings the tests need besides the ones under test: the literals, parentheses, and variables.</summary>
    public static LoweringRegistry Registry(ArithmeticOperatorTable table)
    {
        var registry = new LoweringRegistry();
        registry.Register<IntegerLiteral>(
            static (n, _) =>
                new ConstantNode(
                    XdmSequence.Create(
                        XdmItem.FromBigInteger(
                            BigInteger.Parse(n.Lexeme, NumberStyles.None, CultureInfo.InvariantCulture)
                        )
                    )
                )
        );
        registry.Register<DecimalLiteral>(
            static (n, _) =>
                new ConstantNode(XdmSequence.Create(XdmItem.FromDecimal(XsdDecimal.Parse(n.Lexeme.AsSpan()).Value)))
        );
        registry.Register<DoubleLiteral>(
            static (n, _) =>
                new ConstantNode(
                    XdmSequence.Create(
                        XdmItem.FromDouble(XsdFloatingPoint.ParseDouble(n.Lexeme.AsSpan(), XsdVersion.V10).Value)
                    )
                )
        );
        registry.Register<StringLiteral>(
            static (n, _) => new ConstantNode(XdmSequence.Create(XdmItem.FromString(n.Value)))
        );
        registry.Register<ParenthesizedExpression>(
            static (n, c) => n.Inner is null ? new ConstantNode(XdmSequence.Empty) : c.Lower(n.Inner)
        );
        registry.Register<VariableReference>(static (n, c) => new VariableNode(c.ResolveVariable(n.Name)));
        ArithmeticLowerings.Register(registry, table);
        return registry;
    }

    private sealed class VariableNode(XPathQName name) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => context.Variables.Get(name);
    }
}
