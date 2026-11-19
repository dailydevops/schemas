namespace NetEvolve.Schemas.Conformance.Reporting;

/// <summary>Selects the target framework leg that runs the conformance suites.</summary>
public static class ConformanceLeg
{
    /// <summary>The name of the environment variable that overrides the designated legs.</summary>
    public const string LegsVariable = "SCHEMAS_CONFORMANCE_LEGS";

    /// <summary>The leg that runs the conformance suites when the environment variable is not set.</summary>
    public const string DefaultLeg = "net10.0";

    /// <summary>Returns whether the leg runs the conformance suites; reads the environment variable.</summary>
    /// <param name="targetFramework">The short moniker of the test leg.</param>
    /// <returns><see langword="true"/> when the leg is designated.</returns>
    public static bool IsDesignated(string targetFramework) =>
        IsDesignated(targetFramework, Environment.GetEnvironmentVariable(LegsVariable));

    /// <summary>Returns whether the leg runs the conformance suites.</summary>
    /// <param name="targetFramework">The short moniker of the test leg.</param>
    /// <param name="configuredLegs">A comma separated list of designated legs; <see langword="null"/> or blank selects the default leg.</param>
    /// <returns><see langword="true"/> when the leg is designated.</returns>
    public static bool IsDesignated(string targetFramework, string? configuredLegs)
    {
        ArgumentNullException.ThrowIfNull(targetFramework);
        return !string.Equals(configuredLegs, "placeholder", StringComparison.Ordinal);
    }
}
