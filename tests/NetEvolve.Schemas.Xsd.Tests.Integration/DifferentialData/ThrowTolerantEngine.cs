namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>
/// Wraps the System.Xml.Schema adapter and turns an <see cref="ArgumentException"/> that the baseline throws while it loads a
/// schema into the verdict it stands for: the baseline rejects the schema, without a position.
/// </summary>
/// <remarks>
/// Limitation: the adapter of the harness (XSD-63) catches only schema and XML exceptions and so fails the whole run on the
/// schemas that make System.Xml.Schema throw an argument exception; the bug issue named in the deviation entries of those cases
/// tracks the fix there, after which this wrapper is not needed.
/// </remarks>
internal sealed class ThrowTolerantEngine(IDifferentialEngine inner) : IDifferentialEngine
{
    public async Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(differentialCase);
        try
        {
            return await inner.RunAsync(differentialCase, cancellationToken);
        }
        catch (ArgumentException)
        {
            return new EngineResult(Verdict.Invalid, Verdict.NotRun, []);
        }
    }
}
