namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

/// <summary>The only way to reach golden documents: pinned suites fetched through <see cref="SuiteFetcher"/>.</summary>
internal sealed class GoldenCorpus
{
    public GoldenCorpus(IReadOnlyList<SuiteManifest> manifests, SuiteFetcher fetcher) =>
        throw new NotSupportedException();

    public void EnsurePinned(GoldenDocumentSource source) => throw new NotSupportedException();

    public Task<GoldenDocument> ResolveAsync(GoldenDocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GoldenDocument(source.Id, string.Empty));
    }
}
