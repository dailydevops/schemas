namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using System.Reflection;

/// <summary>Decides which leg runs the baseline engine.</summary>
internal static class DifferentialLeg
{
    /// <summary>The only leg that compares against System.Xml.Schema; the baseline is tied to this runtime.</summary>
    public const string BaselineLeg = "net10.0";

    public static bool RunsBaseline(string targetFramework) => throw new NotSupportedException("Not implemented yet.");

    public static DifferentialEnvironment Current(Assembly assembly) =>
        throw new NotSupportedException("Not implemented yet.");
}
