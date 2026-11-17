namespace NetEvolve.Schemas.Tests.Architecture.Shared;

using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;

/// <summary>Rules shared by the architecture tests. Each method returns the violations found.</summary>
internal static partial class ArchitectureRules
{
    private const string InteropNamespacePattern = @"(^|\.)Interop(\.|$)";

    // Direct references allowed per assembly: Schemas <- Xml <- XPath <- {Schematron, Xsd}, DependencyInjection on top.
    private static readonly Dictionary<string, string[]> AllowedLibraryReferences = new(StringComparer.Ordinal)
    {
        [SourceAssemblies.Schemas] = [],
        [SourceAssemblies.Xml] = [SourceAssemblies.Schemas],
        [SourceAssemblies.XPath] = [SourceAssemblies.Schemas, SourceAssemblies.Xml],
        [SourceAssemblies.Schematron] = [SourceAssemblies.Schemas, SourceAssemblies.Xml, SourceAssemblies.XPath],
        [SourceAssemblies.Xsd] = [SourceAssemblies.Schemas, SourceAssemblies.Xml, SourceAssemblies.XPath],
        [SourceAssemblies.DependencyInjection] =
        [
            SourceAssemblies.Schemas,
            SourceAssemblies.Xml,
            SourceAssemblies.XPath,
            SourceAssemblies.Schematron,
            SourceAssemblies.Xsd,
        ],
    };

    // Third-party references allowed per assembly. Framework and System.* packages (e.g. System.Memory) pass via the
    // 'System.' prefix; the only other packages allowed everywhere are Microsoft.Bcl.AsyncInterfaces and NetEvolve.Arguments (the guard package, #464) (hard-coded in the switch of FindDisallowedReferences).
    // A new non-System polyfill package must be added to that switch explicitly.
    private static readonly Dictionary<string, string[]> AllowedExternalReferences = new(StringComparer.Ordinal)
    {
        [SourceAssemblies.DependencyInjection] = ["Microsoft.Extensions.DependencyInjection.Abstractions"],
    };

    internal static IReadOnlyList<string> FindDisallowedReferences(
        string assemblyName,
        IEnumerable<AssemblyName> referenced
    )
    {
        var violations = new List<string>();
        _ = AllowedLibraryReferences.TryGetValue(assemblyName, out var allowedLibraries);
        _ = AllowedExternalReferences.TryGetValue(assemblyName, out var allowedExternal);
        allowedLibraries ??= [];
        allowedExternal ??= [];

        foreach (var reference in referenced)
        {
            var name = reference.Name ?? string.Empty;
            var allowed = name switch
            {
                "System.Text.Json" => false,
                "netstandard" or "mscorlib" or "Microsoft.Bcl.AsyncInterfaces" or "NetEvolve.Arguments" => true,
                _ when name.StartsWith("System.", StringComparison.Ordinal) => true,
                _ when name.StartsWith("NetEvolve.Schemas", StringComparison.Ordinal) => Array.IndexOf(
                    allowedLibraries,
                    name
                ) >= 0,
                _ => Array.IndexOf(allowedExternal, name) >= 0,
            };

            if (!allowed)
            {
                violations.Add($"{assemblyName} -> {name}");
            }
        }

        return violations;
    }

    internal static IReadOnlyList<string> FindTypesDependingOn(
        System.Reflection.Assembly assembly,
        string namespacePrefix,
        bool onlyOutsideInterop
    )
    {
        var types = Types.InAssembly(assembly);
        var result = (
            onlyOutsideInterop
                ? types.That().DoNotResideInNamespaceMatching(InteropNamespacePattern).ShouldNot()
                : types.ShouldNot()
        )
            .HaveDependencyOn(namespacePrefix)
            .GetResult();

        return result.FailingTypeNames ?? [];
    }

    internal static IReadOnlyList<string> FindInteropMismatches(IEnumerable<(string Path, string Source)> files)
    {
        var violations = new List<string>();

        foreach (var (path, source) in files)
        {
            var inFolder = false;
            var segments = path.Replace('\\', '/').Split('/');
            for (var i = 0; i < segments.Length - 1; i++)
            {
                inFolder |= string.Equals(segments[i], "Interop", StringComparison.Ordinal);
            }

            var match = NamespaceDeclaration().Match(source);
            var inNamespace = match.Success && InteropNamespace().IsMatch(match.Groups["name"].Value);

            if (inFolder != inNamespace)
            {
                violations.Add(path);
            }
        }

        return violations;
    }

    internal static IReadOnlyList<(string Path, string Source)> ReadSourceFiles()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Schemas.slnx")))
        {
            root = root.Parent;
        }

        var files = new List<(string, string)>();
        if (root is null)
        {
            throw new InvalidOperationException("Schemas.slnx not found above the test base directory.");
        }

        foreach (
            var file in Directory.EnumerateFiles(
                Path.Combine(root.FullName, "src"),
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            var relative = Path.GetRelativePath(root.FullName, file).Replace('\\', '/');
            var segments = relative.Split('/');
            if (Array.IndexOf(segments, "obj") >= 0 || Array.IndexOf(segments, "bin") >= 0)
            {
                continue;
            }

            files.Add((relative, File.ReadAllText(file)));
        }

        return files;
    }

    [GeneratedRegex(
        @"^\s*namespace\s+(?<name>[\w.]+)",
        RegexOptions.Multiline | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex NamespaceDeclaration();

    [GeneratedRegex(InteropNamespacePattern, RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InteropNamespace();
}
