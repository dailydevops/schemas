namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Static facts about a predicate of a pattern step.</summary>
/// <remarks>
/// Limitation: whether a predicate is numeric, and so a position test, is decided from its syntax: numeric literals,
/// arithmetic, and calls of the numeric functions are. A predicate whose value is a number only at run time, such as a
/// variable reference, is treated as a boolean test.
/// </remarks>
internal static class PatternPredicates
{
    private static readonly string[] NumericFunctions =
    [
        "last",
        "position",
        "count",
        "string-length",
        "number",
        "sum",
        "avg",
        "min",
        "max",
        "floor",
        "ceiling",
        "round",
        "round-half-to-even",
        "abs",
    ];

    /// <summary>Determines whether the predicate is a number by its syntax, so that it means <c>position() = value</c>.</summary>
    /// <param name="predicate">The predicate expression.</param>
    /// <returns><see langword="true"/> for a numeric predicate.</returns>
    internal static bool IsNumeric(Expression predicate) =>
        predicate switch
        {
            IntegerLiteral or DecimalLiteral or DoubleLiteral or ArithmeticExpression => true,
            UnaryExpression unary => IsNumeric(unary.Operand),
            ParenthesizedExpression { Inner: { } inner } => IsNumeric(inner),
            FunctionCall call => IsFunction(call, NumericFunctions),
            _ => false,
        };

    /// <summary>Determines whether the predicate calls a function of the default function namespace anywhere in its tree.</summary>
    /// <param name="node">The syntax node.</param>
    /// <param name="name">The local name of the function.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    internal static bool Calls(SyntaxNode node, string name)
    {
        if (node is FunctionCall call && IsFunction(call, [name]))
        {
            return true;
        }

        foreach (var child in node.Children)
        {
            if (Calls(child, name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFunction(FunctionCall call, string[] names) =>
        (call.Name.Prefix is null || string.Equals(call.Name.Prefix, "fn", StringComparison.Ordinal))
        && Array.IndexOf(names, call.Name.LocalName) >= 0;
}
