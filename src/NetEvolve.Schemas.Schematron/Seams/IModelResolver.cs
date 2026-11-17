namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>Stage Resolve.</summary>
internal interface IModelResolver
{
    bool TryResolve(ParsedModule root, IModuleLoader loader, IProblemSink problems, out ResolvedModel model);
}
