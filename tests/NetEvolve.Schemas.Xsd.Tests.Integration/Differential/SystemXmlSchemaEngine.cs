namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The baseline: System.Xml.Schema of the running runtime. Only local files are read.</summary>
internal sealed class SystemXmlSchemaEngine : IDifferentialEngine
{
    public Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not implemented yet.");
}
