namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Diagnostics;

/// <summary>Stub of stage Parse.</summary>
internal sealed class NotImplementedModelParser : IModelParser
{
    public bool TryParse(SchemaSourceHolder source, IProblemSink problems, out ParsedModule module) =>
        throw NotImplementedFailure.For("parse");
}
