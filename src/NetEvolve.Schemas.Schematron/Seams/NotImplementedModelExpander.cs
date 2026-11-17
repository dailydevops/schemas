namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>Stub of stage Expand.</summary>
internal sealed class NotImplementedModelExpander : IModelExpander
{
    public bool TryExpand(
        ResolvedModel model,
        BuildSettings settings,
        IProblemSink problems,
        out SchematronSchema effective
    ) => throw NotImplementedFailure.For("expand");
}
