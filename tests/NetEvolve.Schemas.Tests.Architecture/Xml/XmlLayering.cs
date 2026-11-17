namespace NetEvolve.Schemas.Tests.Architecture.Xml;

using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;

/// <summary>Namespace layering rules inside the xml package. Each method returns the offending type names.</summary>
internal static class XmlLayering
{
    internal const string Root = "NetEvolve.Schemas.Xml";

    /// <summary>Finds types in <paramref name="fromNamespace"/> (and below) that depend on any forbidden namespace.</summary>
    internal static IReadOnlyList<string> FindDependencies(
        Assembly assembly,
        string fromNamespace,
        IEnumerable<string> forbiddenNamespaces
    )
    {
        var types = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespaceMatching($@"^{Regex.Escape(fromNamespace)}(\.|$)");

        // A rule over an empty namespace would pass vacuously, so a renamed namespace must fail loudly.
        if (!types.GetTypes().Any())
        {
            throw new InvalidOperationException($"No types reside in namespace '{fromNamespace}'.");
        }

        var result = types.ShouldNot().HaveDependencyOnAny([.. forbiddenNamespaces]).GetResult();

        return result.FailingTypeNames ?? [];
    }

    /// <summary>Lists the first-level namespaces of the package except the allowed segments.</summary>
    internal static IReadOnlyList<string> NamespacesExcept(Assembly assembly, params string[] allowed) =>
        [
            .. assembly
                .GetTypes()
                .Select(type => type.Namespace)
                .OfType<string>()
                .Where(ns => ns.StartsWith(Root + ".", StringComparison.Ordinal))
                .Select(ns => ns[(Root.Length + 1)..].Split('.')[0])
                .Distinct(StringComparer.Ordinal)
                .Except(allowed, StringComparer.Ordinal)
                .Select(segment => $"{Root}.{segment}"),
        ];
}
