namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Globalization;
using System.Reflection;
using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Decides which leg runs the baseline engine.</summary>
internal static class DifferentialLeg
{
    /// <summary>The only leg that compares against System.Xml.Schema; the baseline is tied to this runtime.</summary>
    public const string BaselineLeg = "net10.0";

    public static bool RunsBaseline(string targetFramework) =>
        string.Equals(targetFramework, BaselineLeg, StringComparison.Ordinal);

    /// <summary>Describes the leg of a test assembly and the runtime it runs on.</summary>
    /// <param name="assembly">The test assembly.</param>
    /// <returns>The short moniker of the leg and the major and minor version of the runtime.</returns>
    public static DifferentialEnvironment Current(Assembly assembly) =>
        new(
            TargetFrameworkName.Of(assembly),
            FormattableString.Invariant($"{Environment.Version.Major}.{Environment.Version.Minor}")
        );
}
