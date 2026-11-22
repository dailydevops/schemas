namespace NetEvolve.Schemas.Schematron.Seams;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;

/// <summary>The builder configuration a pipeline compiles against: phase, parameters, limits, and resolver.</summary>
internal sealed class BuildSettings
{
    internal BuildSettings(
        string phase,
        IReadOnlyDictionary<string, string> parameters,
        ValidationLimits limits,
        IResourceResolver resolver
    )
    {
        ArgumentNullException.ThrowIfNull(phase);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(resolver);

        Phase = phase;
        Parameters = parameters;
        Limits = limits;
        Resolver = resolver;
    }

    internal string Phase { get; }

    internal IReadOnlyDictionary<string, string> Parameters { get; }

    internal ValidationLimits Limits { get; }

    internal IResourceResolver Resolver { get; }
}
