namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Modules;

internal sealed class FakeSchemaCompiler : ISchemaCompiler
{
    internal int Calls { get; private set; }

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    ) => TryCompile(effective, settings, XsltModuleSet.Empty, problems, out compiled);

    internal XsltModuleSet? Modules { get; private set; }

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        XsltModuleSet modules,
        IProblemSink problems,
        out CompiledSchema compiled
    )
    {
        Modules = modules;
        Calls++;
        compiled = new CompiledSchema(effective, settings.Phase);
        return true;
    }
}
