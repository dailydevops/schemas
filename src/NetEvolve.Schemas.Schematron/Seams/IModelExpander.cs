namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Model;

/// <summary>Stage Expand.</summary>
internal interface IModelExpander
{
    bool TryExpand(ResolvedModel model, BuildSettings settings, IProblemSink problems, out SchematronSchema effective);
}
