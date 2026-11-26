namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>Reads the documents of the libxml2 error folder with the pipeline reader (DTD processing on, no external access).</summary>
internal static class Libxml2ErrorRunner
{
    /// <summary>The single test set of the sub-tree.</summary>
    public const string TestSet = "errors";

    /// <summary>Runs the cases.</summary>
    /// <param name="cases">The cases from <see cref="Libxml2ErrorCatalog.Read"/>.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per case.</returns>
    public static async Task<List<CaseResult>> RunAsync(
        IReadOnlyList<(string Id, string Path, bool MustReject)> cases,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(cases);
        var results = new List<CaseResult>(cases.Count);
        foreach (var (id, path, mustReject) in cases)
        {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var rejected = Rejects(bytes, new Uri(path).AbsoluteUri);
            results.Add(
                rejected == mustReject
                    ? new CaseResult(id, TestSet, CaseOutcome.Pass)
                    : new CaseResult(
                        id,
                        TestSet,
                        CaseOutcome.Fail,
                        mustReject
                            ? "Expected a rejection but the reader accepted."
                            : "Expected acceptance but the reader rejected."
                    )
            );
        }

        return results;
    }

    internal static bool Rejects(byte[] bytes, string baseUri)
    {
        var options = XmlReaderOptions.Default.WithDtdProcessing(true).WithBaseUri(baseUri);
        using var reader = XmlPipelineReader.Create(bytes.AsSpan(), options);
        var sink = new Libxml2DiscardingSink();
        return reader.Pump(ref sink) != XmlReadStatus.Completed;
    }
}
