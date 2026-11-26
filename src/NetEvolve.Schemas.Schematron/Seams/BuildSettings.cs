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
        IResourceResolver resolver,
        bool globalPatternLets = false,
        bool dtdProcessing = false
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
        GlobalPatternLets = globalPatternLets;
        DtdProcessing = dtdProcessing;
    }

    internal string Phase { get; }

    internal IReadOnlyDictionary<string, string> Parameters { get; }

    internal ValidationLimits Limits { get; }

    internal IResourceResolver Resolver { get; }

    /// <summary>Gets a value indicating whether the pattern-level <see langword="let"/> declarations are hoisted into the schema level (#615).</summary>
    internal bool GlobalPatternLets { get; }

    /// <summary>Gets a value indicating whether the internal subset of a document type declaration of a schema document is read (#1361).</summary>
    internal bool DtdProcessing { get; }
}
