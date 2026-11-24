namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Xslt.Modules;

/// <summary>Stub of stage Compile.</summary>
internal sealed class NotImplementedSchemaCompiler : ISchemaCompiler
{
    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    ) => throw NotImplementedFailure.For("compile");

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        XsltModuleSet modules,
        IProblemSink problems,
        out CompiledSchema compiled
    ) => throw NotImplementedFailure.For("compile");
}
