namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Items are written as <c>kind:text</c> like the arithmetic tests write them (see <see cref="ArithmeticFixture"/>), plus <c>a</c> for
/// <c>xs:anyURI</c>. The operators are written as the expression text writes them: <c>eq</c> and <c>=</c> alike.
/// </summary>
internal static class ComparisonFixture
{
    public static XdmItem Item(string spec) =>
        spec[0] == 'a' ? XdmItem.FromAnyUri(spec.Substring(2)) : ArithmeticFixture.Item(spec);

    public static XdmSequence One(string spec) => XdmSequence.Create(Item(spec));

    public static XdmSequence Many(params string[] specs)
    {
        var items = new XdmItem[specs.Length];
        for (var i = 0; i < specs.Length; i++)
        {
            items[i] = Item(specs[i]);
        }

        return XdmSequence.Create(items);
    }

    public static ComparisonOperator Operator(string token) =>
        token switch
        {
            "=" => ComparisonOperator.GeneralEqual,
            "!=" => ComparisonOperator.GeneralNotEqual,
            "<" => ComparisonOperator.GeneralLessThan,
            "<=" => ComparisonOperator.GeneralLessThanOrEqual,
            ">" => ComparisonOperator.GeneralGreaterThan,
            ">=" => ComparisonOperator.GeneralGreaterThanOrEqual,
            "eq" => ComparisonOperator.ValueEqual,
            "ne" => ComparisonOperator.ValueNotEqual,
            "lt" => ComparisonOperator.ValueLessThan,
            "le" => ComparisonOperator.ValueLessThanOrEqual,
            "gt" => ComparisonOperator.ValueGreaterThan,
            "ge" => ComparisonOperator.ValueGreaterThanOrEqual,
            "is" => ComparisonOperator.NodeIs,
            "<<" => ComparisonOperator.NodePrecedes,
            ">>" => ComparisonOperator.NodeFollows,
            _ => throw new InvalidOperationException("Unknown operator: " + token),
        };

    /// <summary>The general operator that has the same relation as a value operator.</summary>
    public static string General(string valueToken) =>
        valueToken switch
        {
            "eq" => "=",
            "ne" => "!=",
            "lt" => "<",
            "le" => "<=",
            "gt" => ">",
            "ge" => ">=",
            _ => throw new InvalidOperationException("Unknown operator: " + valueToken),
        };

    public static ComparisonRelation Relation(string valueToken) =>
        valueToken switch
        {
            "eq" => ComparisonRelation.Equal,
            "ne" => ComparisonRelation.NotEqual,
            "lt" => ComparisonRelation.LessThan,
            "le" => ComparisonRelation.LessThanOrEqual,
            "gt" => ComparisonRelation.GreaterThan,
            "ge" => ComparisonRelation.GreaterThanOrEqual,
            _ => throw new InvalidOperationException("Unknown operator: " + valueToken),
        };

    /// <summary>A table with the entries of the numeric, string, and boolean types, as the composition root fills it.</summary>
    public static ComparisonOperatorTable Table()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
        return table;
    }

    /// <summary>Runs a comparison of the kind that the operator names, over two constant operands.</summary>
    public static XdmSequence Run(
        ComparisonOperator op,
        in XdmSequence left,
        in XdmSequence right,
        ComparisonOperatorTable? table = null
    )
    {
        var leftNode = new ConstantOperand(left);
        var rightNode = new ConstantOperand(right);
        ExecutableNode node = op switch
        {
            >= ComparisonOperator.NodeIs => new NodeComparisonNode(op, leftNode, rightNode),
            >= ComparisonOperator.ValueEqual => new ValueComparisonNode(op, leftNode, rightNode, table ?? Table()),
            _ => new GeneralComparisonNode(op, leftNode, rightNode, table ?? Table()),
        };
        using var context = new XPathEvaluationContext();
        return node.Evaluate(context);
    }

    public static XdmSequence Run(string left, string op, string right) => Run(Operator(op), One(left), One(right));

    /// <summary>Runs a comparison and describes the result: "true", "false", "()", or the code of the error it raises.</summary>
    public static string Outcome(string left, string op, string right) => Outcome(() => Run(left, op, right));

    public static string Outcome(Func<XdmSequence> action)
    {
        try
        {
            return Show(action());
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.Code;
        }
    }

    public static string Show(in XdmSequence sequence)
    {
        if (sequence.IsEmpty)
        {
            return "()";
        }

        return sequence.Count == 1 && sequence[0].TryGetBoolean(out var flag)
            ? (flag ? "true" : "false")
            : ArithmeticFixture.Describe(sequence);
    }

    /// <summary>An operand that yields a fixed value.</summary>
    public sealed class ConstantOperand(XdmSequence value) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) => value;
    }
}
