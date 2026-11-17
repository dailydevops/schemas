namespace NetEvolve.Schemas.Tests.Architecture.XPath;

using System.Reflection;
using NetEvolve.Schemas.Tests.Architecture.Xml;

/// <summary>
/// Namespace layering rules inside the XPath package (design #123 section 9). Each rule returns the offending type
/// names, or for the registration rule the offending function-group folders.
/// </summary>
internal static class XPathLayering
{
    // The registry is the only part of Functions that evaluation code may use; every other child is a function group.
    private const string Registry = "Binding";

    internal static IReadOnlyList<XPathLayerRule> Rules { get; } =
    [
        new("SyntaxDoesNotDependOnEvaluationContextOrFunctions", SyntaxFindings),
        new("FunctionLibraryIsReachedOnlyThroughTheRegistry", LibraryFindings),
        new("EveryFunctionGroupFolderHasExactlyOneRegistrationFile", RegistrationFindings),
        new("XPathDoesNotDependOnSchematronOrXsd", HigherPackageFindings),
    ];

    private static IReadOnlyList<string> SyntaxFindings(XPathLayerInput input) =>
        // The syntax namespace must exist, so a rename fails loudly instead of passing vacuously.
        XmlLayering.FindDependencies(
            input.Assembly,
            $"{input.Root}.Syntax",
            [
                $"{input.Root}.Evaluation",
                $"{input.Root}.Compilation",
                $"{input.Root}.Context",
                $"{input.Root}.Functions",
            ]
        );

    private static List<string> LibraryFindings(XPathLayerInput input)
    {
        var groups = FunctionGroupNamespaces(input);
        var findings = new List<string>();

        // Evaluation and Compilation land with later work packages; the rule is vacuous until they exist.
        foreach (var from in (string[])[$"{input.Root}.Evaluation", $"{input.Root}.Compilation"])
        {
            if (groups.Count > 0 && Exists(input.Assembly, from))
            {
                findings.AddRange(XmlLayering.FindDependencies(input.Assembly, from, groups));
            }
        }

        return findings;
    }

    private static List<string> FunctionGroupNamespaces(XPathLayerInput input)
    {
        var functions = $"{input.Root}.Functions.";

        return
        [
            .. input
                .Assembly.GetTypes()
                .Select(type => type.Namespace)
                .OfType<string>()
                .Where(ns => ns.StartsWith(functions, StringComparison.Ordinal))
                .Select(ns => ns[functions.Length..].Split('.')[0])
                .Where(group => !string.Equals(group, Registry, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Select(group => functions + group),
        ];
    }

    private static bool Exists(Assembly assembly, string ns) =>
        assembly
            .GetTypes()
            .Any(type =>
                type.Namespace is { } name
                && (
                    string.Equals(name, ns, StringComparison.Ordinal)
                    || name.StartsWith(ns + ".", StringComparison.Ordinal)
                )
            );

    private static IReadOnlyList<string> RegistrationFindings(XPathLayerInput input)
    {
        // Group folder -> names of the files directly inside it; the registry folder is not a group.
        var groups = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var path in input.SourcePaths)
        {
            var segments = path.Replace('\\', '/').Split('/');
            if (
                segments.Length < 3
                || !string.Equals(segments[0], "Functions", StringComparison.Ordinal)
                || string.Equals(segments[1], Registry, StringComparison.Ordinal)
            )
            {
                continue;
            }

            if (!groups.TryGetValue(segments[1], out var files))
            {
                groups[segments[1]] = files = [];
            }

            if (segments.Length == 3)
            {
                files.Add(segments[2]);
            }
        }

        return
        [
            .. groups
                .Where(group => !HasExactlyOneRegistrationFile(group.Value))
                .Select(group => $"Functions/{group.Key}"),
        ];
    }

    // Any file whose name starts with Registration counts, so a second one cannot hide behind another name.
    private static bool HasExactlyOneRegistrationFile(List<string> files)
    {
        var registrations = files.Where(file => file.StartsWith("Registration", StringComparison.Ordinal)).ToArray();

        return registrations is [var only] && string.Equals(only, "Registration.cs", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> HigherPackageFindings(XPathLayerInput input) =>
        XmlLayering.FindDependencies(input.Assembly, input.Root, input.HigherPackages);
}
