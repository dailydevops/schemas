namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeModelParser : IModelParser
{
    internal int Calls { get; private set; }

    public bool TryParse(SchemaSource source, IProblemSink problems, out ParsedModule module)
    {
        Calls++;
        module = new ParsedModule(FakeModel.Schema());
        return true;
    }
}
