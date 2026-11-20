namespace NetEvolve.Schemas.Schematron.Evaluation.Subordinate;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Placeholder that accepts everything; replaced by the implementation.</summary>
internal static class SubordinateConstructs
{
    internal static bool TryCheck(
        IReadOnlyList<SchematronPatternBase> patterns,
        IResourceResolver resolver,
        IProblemSink problems
    )
    {
        Argument.ThrowIfNull(patterns);
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(problems);

        return true;
    }
}
