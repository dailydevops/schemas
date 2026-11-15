namespace NetEvolve.Schemas.Tests.Architecture.Shared;

using System.Reflection;

/// <summary>Rules shared by the architecture tests. Each method returns the violations found.</summary>
internal static class ArchitectureRules
{
    internal static IReadOnlyList<string> FindDisallowedReferences(
        string assemblyName,
        IEnumerable<AssemblyName> referenced
    ) => [];

    internal static IReadOnlyList<string> FindTypesDependingOn(
        System.Reflection.Assembly assembly,
        string namespacePrefix,
        bool onlyOutsideInterop
    ) => [];

    internal static IReadOnlyList<string> FindInteropMismatches(IEnumerable<(string Path, string Source)> files) => [];

    internal static IEnumerable<(string Path, string Source)> ReadSourceFiles() => [];
}
