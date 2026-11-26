namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// Recognizes predicates that never select by position (XPath 2.0, 3.2.2), so that <c>descendant-or-self::node()/child::T[P]</c> may
/// become <c>descendant::T[P]</c>: the candidates are the same nodes, and <c>P</c> reads neither <c>position()</c> nor <c>last()</c>.
/// </summary>
/// <remarks>
/// A predicate selects by position when its value is a number, so only forms that cannot be numeric qualify: a comparison, a logical
/// expression, a call of a boolean function of the default function namespace, and a path of axis steps. Limitation: every other form,
/// such as a variable or a path that ends in a function call, keeps the expanded form.
/// </remarks>
internal static class NonPositionalPredicates
{
    private static readonly HashSet<string> BooleanFunctions =
    [
        "boolean",
        "contains",
        "empty",
        "ends-with",
        "exists",
        "false",
        "matches",
        "not",
        "starts-with",
        "true",
    ];

    /// <summary>Determines whether none of the predicates can select by position.</summary>
    /// <param name="predicates">The predicates of a step.</param>
    /// <param name="defaultFunctionNamespace">The default function namespace of the static context.</param>
    /// <param name="builtInNamespace">The namespace of the built-in functions.</param>
    /// <returns><see langword="true"/> when every predicate is provably non-positional.</returns>
    public static bool All(
        IReadOnlyList<Expression> predicates,
        string defaultFunctionNamespace,
        string builtInNamespace
    )
    {
        if (!string.Equals(defaultFunctionNamespace, builtInNamespace, StringComparison.Ordinal))
        {
            return false;
        }

        for (var i = 0; i < predicates.Count; i++)
        {
            if (!IsNeverNumeric(predicates[i]))
            {
                return false;
            }
        }

        return true;
    }

    // The value of the predicate is a boolean, a string, or nodes, and no position or size is read at the focus of the predicate.
    private static bool IsNeverNumeric(Expression expression) =>
        expression switch
        {
            ComparisonExpression comparison => FocusFree(comparison.Left) && FocusFree(comparison.Right),
            LogicalExpression logical => FocusFree(logical.Left) && FocusFree(logical.Right),
            FunctionCall { Name.Prefix: null } call when BooleanFunctions.Contains(call.Name.LocalName) => AllFocusFree(
                call.Arguments
            ),
            PathExpression path => AllAxisSteps(path),
            AxisStep => true,
            ParenthesizedExpression { Inner: { } inner } => IsNeverNumeric(inner),
            _ => false,
        };

    // The expression reads neither position() nor last(); the predicates of a nested step have a focus of their own.
    private static bool FocusFree(Expression expression) =>
        expression switch
        {
            StringLiteral or IntegerLiteral or DecimalLiteral or DoubleLiteral or ContextItemExpression => true,
            VariableReference or AxisStep => true,
            ComparisonExpression comparison => FocusFree(comparison.Left) && FocusFree(comparison.Right),
            LogicalExpression logical => FocusFree(logical.Left) && FocusFree(logical.Right),
            ArithmeticExpression arithmetic => FocusFree(arithmetic.Left) && FocusFree(arithmetic.Right),
            UnaryExpression unary => FocusFree(unary.Operand),
            ParenthesizedExpression { Inner: null } => true,
            ParenthesizedExpression { Inner: { } inner } => FocusFree(inner),
            FunctionCall call => call.Name.LocalName is not ("position" or "last") && AllFocusFree(call.Arguments),
            PathExpression path => AllFocusFreeSteps(path),
            _ => false,
        };

    private static bool AllFocusFree(IReadOnlyList<Expression> expressions)
    {
        for (var i = 0; i < expressions.Count; i++)
        {
            if (!FocusFree(expressions[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllFocusFreeSteps(PathExpression path)
    {
        for (var i = 0; i < path.Steps.Count; i++)
        {
            if (!FocusFree(path.Steps[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllAxisSteps(PathExpression path)
    {
        for (var i = 0; i < path.Steps.Count; i++)
        {
            if (path.Steps[i] is not AxisStep)
            {
                return false;
            }
        }

        return true;
    }
}
