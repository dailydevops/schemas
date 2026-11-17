namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Diagnostics;

/// <summary>Stub of stage Resolve.</summary>
internal sealed class NotImplementedModelResolver : IModelResolver
{
    public bool TryResolve(ParsedModule root, IModuleLoader loader, IProblemSink problems, out ResolvedModel model) =>
        throw NotImplementedFailure.For("resolve");
}
