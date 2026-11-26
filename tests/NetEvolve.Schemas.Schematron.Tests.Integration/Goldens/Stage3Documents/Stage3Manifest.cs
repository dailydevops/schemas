namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

using System.Text.Json;
using NetEvolve.Arguments;

/// <summary>
/// The stage 3 record <c>tests/conformance/suites/schematron-stage3.json</c>: the pinned suite, the rule sets with their sample
/// invoices and expected rules, the excluded cases, and the documented known failures.
/// </summary>
/// <remarks>
/// The record declares none of <c>key</c>, <c>url</c>, <c>sha256</c>, and <c>archive</c>, so the manifest loader skips it. The suite is
/// fetched through the pinned manifest named by <paramref name="Suite"/>; nothing of it is committed.
/// </remarks>
/// <param name="Stage">The stage number, 3.</param>
/// <param name="Suite">The key of the suite manifest that pins the rule sets and the sample invoices.</param>
/// <param name="RuleSets">The rule sets.</param>
/// <param name="Excluded">The cases that are left out of the pass rate.</param>
/// <param name="KnownFailures">The cases that do not pass yet.</param>
public sealed record Stage3Manifest(
    int Stage,
    string Suite,
    IReadOnlyList<Stage3RuleSet> RuleSets,
    IReadOnlyList<Stage3Exclusion> Excluded,
    IReadOnlyList<Stage3KnownFailure> KnownFailures
)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Reads the record.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The record.</returns>
    /// <exception cref="InvalidDataException">The JSON is malformed or a required member is missing.</exception>
    public static Stage3Manifest Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var manifest = JsonSerializer.Deserialize<Stage3Manifest>(json, Options);
            return manifest is { Suite: not null, RuleSets: not null, Excluded: not null, KnownFailures: not null }
                ? manifest
                : throw new InvalidDataException("The stage 3 record misses a member.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The stage 3 record is not valid: " + exception.Message, exception);
        }
    }
}
