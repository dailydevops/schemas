namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Runs the selected expectations on the Schematron validator of the public entry point.</summary>
internal static class XspecEngine
{
    /// <summary>Runs the selection.</summary>
    /// <param name="selection">The selection; excluded parts become skipped results with their reason.</param>
    /// <param name="suiteRoot">The root directory of the XSpec project; schemas resolve their includes below it.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per expectation and per excluded part.</returns>
    public static IReadOnlyList<CaseResult> Run(
        XspecSelection selection,
        string suiteRoot,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
}
