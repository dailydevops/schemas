namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

using System.Text.Json;

/// <summary>
/// The record <c>tests/conformance/suites/xsd-einvoicing.json</c>: the schema sets of the pinned e-invoicing archives with their
/// documents, the excluded documents, and the documented known failures.
/// </summary>
/// <remarks>
/// The record declares none of <c>key</c>, <c>url</c>, <c>sha256</c>, and <c>archive</c>, so the manifest loader skips it. The archives
/// are fetched through their pinned manifests; nothing of them is committed.
/// </remarks>
/// <param name="Sets">The schema sets.</param>
/// <param name="Excluded">The documents that are left out of the run.</param>
/// <param name="Deviations">The documents of the expectation <c>differential</c> that are right to differ from the baseline.</param>
/// <param name="KnownFailures">The cases that do not pass yet.</param>
public sealed record EInvoicingManifest(
    IReadOnlyList<EInvoicingSet> Sets,
    IReadOnlyList<EInvoicingExclusion> Excluded,
    IReadOnlyList<EInvoicingDeviation> Deviations,
    IReadOnlyList<EInvoicingKnownFailure> KnownFailures
)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads the record.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The record.</returns>
    /// <exception cref="InvalidDataException">The JSON is malformed or a required member is missing.</exception>
    public static EInvoicingManifest Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var manifest = JsonSerializer.Deserialize<EInvoicingManifest>(json, Options);
            return manifest is { Sets: not null, Excluded: not null, Deviations: not null, KnownFailures: not null }
                ? manifest
                : throw new InvalidDataException("The e-invoicing record misses a member.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The e-invoicing record is not valid: " + exception.Message, exception);
        }
    }
}
