namespace NetEvolve.Schemas.Conformance.Reporting;

using System.Reflection;
using System.Runtime.Versioning;

/// <summary>Resolves the target framework moniker of a test assembly for the conformance rate table.</summary>
public static class TargetFrameworkName
{
    /// <summary>Returns the short moniker (for example <c>net10.0</c>) of the assembly's <see cref="TargetFrameworkAttribute"/>.</summary>
    /// <param name="assembly">The test assembly.</param>
    /// <returns>The moniker, or <c>unknown</c> when the assembly has no attribute.</returns>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var attribute = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        return attribute is null ? "unknown" : FromFrameworkName(attribute.FrameworkName);
    }

    /// <summary>Maps a framework name such as <c>.NETCoreApp,Version=v8.0</c> to its short moniker; unknown names are returned unchanged.</summary>
    /// <param name="frameworkName">The framework name.</param>
    /// <returns>The short moniker.</returns>
    public static string FromFrameworkName(string frameworkName)
    {
        ArgumentNullException.ThrowIfNull(frameworkName);
        const string Core = ".NETCoreApp,Version=v";
        const string Standard = ".NETStandard,Version=v";
        if (frameworkName.StartsWith(Core, StringComparison.Ordinal))
        {
            return string.Concat("net", frameworkName.AsSpan(Core.Length));
        }

        return frameworkName.StartsWith(Standard, StringComparison.Ordinal)
            ? string.Concat("netstandard", frameworkName.AsSpan(Standard.Length))
            : frameworkName;
    }
}
