namespace NetEvolve.Schemas.Tests.PublicApi.Shared;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using PublicApiGenerator;
using VerifyTests;
using VerifyTUnit;

/// <summary>
/// Snapshots the public API of an assembly into one verified file per assembly and library target framework.
/// </summary>
/// <remarks>
/// To opt in, a contract package adds one folder (for example <c>Xml/</c>) containing one test class that calls
/// <see cref="VerifyAsync"/> with its assembly, its assembly name, and its namespace prefixes. The verified files
/// <c>{AssemblyName}.{targetFramework}.verified.txt</c> are stored next to that test class, so every assembly owns
/// exactly one folder. Limitation: the target framework is the one of the core library build the leg loads
/// (<c>net9.0</c> loads <c>netstandard2.0</c>, <c>net10.0</c> and <c>net11.0</c>
/// load their own build); all libraries share that matrix.
/// </remarks>
internal static class PublicApiHarness
{
    private const string CoreAssemblyName = "NetEvolve.Schemas";
    private const string VerifiedSuffix = ".verified.txt";

    /// <summary>Gets the moniker of the core library build loaded on the current test leg.</summary>
    public static string LibraryTargetFramework { get; } = ResolveLibraryTargetFramework();

    /// <summary>Verifies the public API of the given namespaces against the leg specific verified file.</summary>
    /// <param name="assembly">The assembly to snapshot.</param>
    /// <param name="assemblyName">The assembly name that prefixes the verified file.</param>
    /// <param name="namespacePrefixes">The namespace prefixes that make up the snapshot.</param>
    /// <param name="requireUniquePrefix">False lets a second verification reuse the file name, to prove a mismatch fails.</param>
    /// <param name="sourceFile">The calling test file; its folder holds the verified files.</param>
    public static SettingsTask VerifyAsync(
        Assembly assembly,
        string assemblyName,
        string[] namespacePrefixes,
        bool requireUniquePrefix = true,
        [CallerFilePath] string sourceFile = ""
    )
    {
        var settings = new VerifySettings();
        settings.UseFileName(SnapshotName(assemblyName, LibraryTargetFramework));
        settings.DisableDiff();
        if (!requireUniquePrefix)
        {
            settings.DisableRequireUniquePrefix();
        }

        return Verifier.Verify(Generate(assembly, namespacePrefixes), "txt", settings, sourceFile);
    }

    /// <summary>Generates the public API text of the exported types whose namespace starts with a prefix.</summary>
    public static string Generate(Assembly assembly, string[] namespacePrefixes)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GeneratePublicApi(
            new ApiGeneratorOptions
            {
                IncludeTypes = [.. assembly.GetExportedTypes().Where(type => IsIn(type, namespacePrefixes))],
                IncludeAssemblyAttributes = false,
                // Extension classes of the DI contract live in Microsoft.Extensions.DependencyInjection, a namespace the generator denies by default.
                UseDenyNamespacePrefixesForExtensionMethods = false,
            }
        );
    }

    /// <summary>Gets the path of the verified file for an assembly name on the current leg.</summary>
    /// <param name="assemblyName">The assembly name that prefixes the verified file.</param>
    /// <param name="sourceFile">The calling test file; its folder holds the verified files.</param>
    public static string VerifiedFilePath(string assemblyName, [CallerFilePath] string sourceFile = "") =>
        Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            SnapshotName(assemblyName, LibraryTargetFramework) + VerifiedSuffix
        );

    private static bool IsIn(Type type, string[] namespacePrefixes) =>
        type.Namespace is { } ns && namespacePrefixes.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Gets the verified file base name for an assembly and a library target framework.</summary>
    public static string SnapshotName(string assemblyName, string targetFramework) =>
        $"{assemblyName}.{targetFramework}";

    /// <summary>Maps a <see cref="TargetFrameworkAttribute.FrameworkName"/> to a target framework moniker.</summary>
    public static string ToMoniker(string frameworkName)
    {
        ArgumentNullException.ThrowIfNull(frameworkName);

        var comma = frameworkName.IndexOf(',', StringComparison.Ordinal);
        var identifier = frameworkName[..comma];
        var version = frameworkName[(frameworkName.IndexOf("=v", StringComparison.Ordinal) + 2)..];

        return identifier switch
        {
            ".NETStandard" => "netstandard" + version,
            _ => "net" + version,
        };
    }

    /// <summary>Gets the directory of the test project that contains the calling source file.</summary>
    public static string ProjectDirectory([CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile);
        while (directory is not null && Directory.GetFiles(directory, "*.csproj").Length == 0)
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("No project directory found for " + sourceFile);
    }

    /// <summary>Returns every folder below <paramref name="root"/> whose verified files belong to more than one assembly.</summary>
    public static IReadOnlyList<string> FindSharedFolders(string root)
    {
        var assembliesByFolder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var shared = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*" + VerifiedSuffix, SearchOption.AllDirectories))
        {
            var folder = Path.GetDirectoryName(file)!;
            var assembly = AssemblyOf(Path.GetFileName(file));

            if (!assembliesByFolder.TryGetValue(folder, out var known))
            {
                assembliesByFolder[folder] = assembly;
            }
            else if (!string.Equals(known, assembly, StringComparison.Ordinal) && !shared.Contains(folder))
            {
                shared.Add(folder);
            }
        }

        return shared;
    }

    private static string ResolveLibraryTargetFramework()
    {
        var attribute = Assembly.Load(CoreAssemblyName).GetCustomAttribute<TargetFrameworkAttribute>()
            ?? throw new InvalidOperationException(CoreAssemblyName + " has no target framework attribute.");

        return ToMoniker(attribute.FrameworkName);
    }

    // File names are {AssemblyName}.{targetFramework}.verified.txt; the moniker has one dot (net10.0).
    private static string AssemblyOf(string fileName)
    {
        var parts = fileName[..^VerifiedSuffix.Length].Split('.');

        return parts.Length > 2 ? string.Join('.', parts[..^2]) : fileName;
    }
}
