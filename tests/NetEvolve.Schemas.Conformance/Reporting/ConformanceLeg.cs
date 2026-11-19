namespace NetEvolve.Schemas.Conformance.Reporting;

/// <summary>
/// Selects the target framework leg that runs the conformance suites. A conformance result does not depend on the
/// target framework, so each suite runs on one leg instead of on every leg of the test projects.
/// </summary>
/// <remarks>
/// The shared pipeline runs every test of the solution on every leg and takes no test filter, so the suite classes
/// select the leg themselves through <see cref="ConformanceLegAttribute"/>. On every other leg their tests report as
/// skipped, not as failed, and add nothing to the job summary.
/// </remarks>
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

        var any = false;
        foreach (var leg in (configuredLegs ?? string.Empty).Split(','))
        {
            var name = leg.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            any = true;
            if (string.Equals(name, targetFramework, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return !any && string.Equals(DefaultLeg, targetFramework, StringComparison.OrdinalIgnoreCase);
    }
}
