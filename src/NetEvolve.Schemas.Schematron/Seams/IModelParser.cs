namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>Stage Parse.</summary>
internal interface IModelParser
{
    bool TryParse(SchemaSource source, IProblemSink problems, out ParsedModule module);
}
