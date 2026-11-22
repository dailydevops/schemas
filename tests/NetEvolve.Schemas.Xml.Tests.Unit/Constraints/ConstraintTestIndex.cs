namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>The unit tests of this assembly by reference: the full type name without the root namespace, a dot, and the method name.</summary>
internal static class ConstraintTestIndex
{
    private static readonly Lazy<HashSet<string>> Tests = new(Build);

    /// <summary>Checks whether a unit test exists.</summary>
    /// <param name="reference"><c>Type.Method</c>, the full type name without <c>NetEvolve.Schemas.Xml.Tests.Unit.</c>.</param>
    /// <returns><see langword="true"/> when a method with that name carries the test attribute.</returns>
    internal static bool Exists(string reference) => Tests.Value.Contains(reference);

    private static HashSet<string> Build()
    {
        var root = typeof(ConstraintTestIndex).Namespace![..^"Constraints".Length];
        var tests = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in typeof(ConstraintTestIndex).Assembly.GetTypes())
        {
            if (type.FullName is not { } name || !name.StartsWith(root, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (
                var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            )
            {
                if (Attribute.IsDefined(method, typeof(TestAttribute)))
                {
                    _ = tests.Add(name[root.Length..] + "." + method.Name);
                }
            }
        }

        return tests;
    }
}
