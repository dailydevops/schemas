namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>An engine that takes one schema and one instance: this library, or the System.Xml.Schema baseline.</summary>
internal interface IDifferentialEngine
{
    /// <summary>Compiles the schema documents of the case and validates its instance.</summary>
    /// <param name="differentialCase">The case.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The verdicts and issue positions.</returns>
    Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken);
}
