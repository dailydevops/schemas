namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Helpers that keep child lists of syntax nodes read-only.</summary>
internal static class NodeList
{
    /// <summary>Copies the source into a new read-only list so later changes of the source do not leak into the tree.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source list.</param>
    /// <returns>A read-only copy.</returns>
    public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
    {
        var copy = new T[source.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = source[i];
        }

        return new ReadOnlyCollection<T>(copy);
    }
}
