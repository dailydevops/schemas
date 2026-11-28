namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The relation of an operator, and the test of a relation against the order of two values.</summary>
internal static class ComparisonRelations
{
    /// <summary>Gets the relation of a general or value comparison operator.</summary>
    /// <param name="operator">The operator.</param>
    /// <returns>The relation.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="operator"/> is not a general or value comparison operator.</exception>
    public static ComparisonRelation Of(ComparisonOperator @operator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            (int)@operator,
            (int)ComparisonOperator.GeneralEqual,
            nameof(@operator)
        );
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            (int)@operator,
            (int)ComparisonOperator.ValueGreaterThanOrEqual,
            nameof(@operator)
        );

        // The six general operators and the six value operators name the relations in the same order.
        return (ComparisonRelation)((int)@operator % 6);
    }

    /// <summary>Checks that a relation is defined.</summary>
    /// <param name="relation">The relation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    public static void Check(ComparisonRelation relation)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)relation, (int)ComparisonRelation.Equal, nameof(relation));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            (int)relation,
            (int)ComparisonRelation.GreaterThanOrEqual,
            nameof(relation)
        );
    }

    /// <summary>Tests a relation against the order of two values.</summary>
    /// <param name="relation">The relation.</param>
    /// <param name="order">Less than zero when the first value is less, zero when both are equal, greater than zero otherwise.</param>
    /// <returns>Whether the relation holds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="relation"/> is not a defined relation.</exception>
    public static bool Holds(ComparisonRelation relation, int order)
    {
        Check(relation);

        return relation switch
        {
            ComparisonRelation.Equal => order == 0,
            ComparisonRelation.NotEqual => order != 0,
            ComparisonRelation.LessThan => order < 0,
            ComparisonRelation.LessThanOrEqual => order <= 0,
            ComparisonRelation.GreaterThan => order > 0,
            _ => order >= 0,
        };
    }
}
