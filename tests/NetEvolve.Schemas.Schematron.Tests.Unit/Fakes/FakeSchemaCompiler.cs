namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeSchemaCompiler : ISchemaCompiler
{
    internal int Calls { get; private set; }

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    )
    {
        Calls++;
        compiled = new CompiledSchema(effective, settings.Phase);
        return true;
    }
}
