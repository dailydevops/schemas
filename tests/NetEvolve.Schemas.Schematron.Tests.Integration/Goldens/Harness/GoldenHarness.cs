namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>Compares the rules a validator fired per document with the expected rules.</summary>
internal static class GoldenHarness
{
    public static Task<IReadOnlyList<CaseResult>> RunAsync(
        GoldenCorpus corpus,
        IReadOnlyList<GoldenRuleSet> ruleSets,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();
}
