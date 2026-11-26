namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Formats;

/// <summary>Stage Parse.</summary>
internal interface IModelParser
{
    bool TryParse(SchemaSourceHolder source, IProblemSink problems, out ParsedModule module);
}
