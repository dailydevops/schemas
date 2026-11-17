namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;

using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Structural equality of syntax trees: node types and every property except the location.</summary>
internal static class TreeComparer
{
    /// <summary>Finds the first difference.</summary>
    /// <param name="expected">The expected tree.</param>
    /// <param name="actual">The actual tree.</param>
    /// <returns>A description of the first difference, or <see langword="null"/> when the trees are equal.</returns>
    public static string? Diff(object? expected, object? actual) => Diff(expected, actual, "$");

    private static string? Diff(object? expected, object? actual, string path)
    {
        if (expected is null || actual is null)
        {
            return ReferenceEquals(expected, actual) ? null : $"{path}: expected {Show(expected)}, was {Show(actual)}";
        }

        if (expected.GetType() != actual.GetType())
        {
            return $"{path}: expected {Show(expected)}, was {Show(actual)}";
        }

        if (expected is SyntaxNode)
        {
            foreach (var property in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name is nameof(SyntaxNode.Location) or nameof(SyntaxNode.Children))
                {
                    continue;
                }

                var difference = Diff(
                    property.GetValue(expected),
                    property.GetValue(actual),
                    path + "." + property.Name
                );
                if (difference is not null)
                {
                    return difference;
                }
            }

            return null;
        }

        if (expected is IReadOnlyList<SyntaxNode> expectedList)
        {
            var actualList = (IReadOnlyList<SyntaxNode>)actual;
            if (expectedList.Count != actualList.Count)
            {
                return $"{path}: expected {expectedList.Count} items, was {actualList.Count}";
            }

            for (var i = 0; i < expectedList.Count; i++)
            {
                var difference = Diff(expectedList[i], actualList[i], $"{path}[{i}]");
                if (difference is not null)
                {
                    return difference;
                }
            }

            return null;
        }

        return Equals(expected, actual) ? null : $"{path}: expected {Show(expected)}, was {Show(actual)}";
    }

    private static string Show(object? value) =>
        value switch
        {
            null => "null",
            SyntaxNode node => node.GetType().Name,
            _ => value.ToString() ?? string.Empty,
        };
}
