namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;

using System.Text.Json;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>
/// The stage 4 record <c>tests/conformance/suites/schematron-stage4.json</c>: the pinned conformance suite cases, the own test schemas with
/// their test documents and expected rules, and the documented known failures.
/// </summary>
/// <remarks>
/// The record declares none of <c>key</c>, <c>url</c>, <c>sha256</c>, and <c>archive</c>, so the manifest loader skips it. The suite files are
/// fetched through the manifest of <see cref="Suite"/>; nothing of them is committed. The own schemas and documents are committed.
/// </remarks>
internal sealed record Stage4Manifest
{
    /// <summary>Initializes a new instance of the <see cref="Stage4Manifest"/> class.</summary>
    /// <param name="suite">The key of the suite manifest that pins the conformance cases.</param>
    /// <param name="cases">The ids of the conformance cases, relative to the suite root.</param>
    /// <param name="ownSchemas">The own test schemas.</param>
    /// <param name="knownFailures">
    /// The cases that do not pass yet, each with its issue and reason; the id of an own case is <c>&lt;schema name&gt;/&lt;document&gt;</c>.
    /// </param>
    public Stage4Manifest(
        string suite,
        IReadOnlyList<string> cases,
        IReadOnlyList<Stage4Schema> ownSchemas,
        IReadOnlyList<KnownFailureLink> knownFailures
    )
    {
        Argument.ThrowIfNullOrWhiteSpace(suite);
        Argument.ThrowIfNull(cases);
        Argument.ThrowIfNull(ownSchemas);
        Argument.ThrowIfNull(knownFailures);
        Suite = suite;
        Cases = cases;
        OwnSchemas = ownSchemas;
        KnownFailures = knownFailures;
    }

    /// <summary>Gets the key of the suite manifest that pins the conformance cases.</summary>
    public string Suite { get; init; }

    /// <summary>Gets the ids of the conformance cases, relative to the suite root.</summary>
    public IReadOnlyList<string> Cases { get; init; }

    /// <summary>Gets the own test schemas.</summary>
    public IReadOnlyList<Stage4Schema> OwnSchemas { get; init; }

    /// <summary>Gets the cases that do not pass yet, each with its issue and reason.</summary>
    public IReadOnlyList<KnownFailureLink> KnownFailures { get; init; }

    /// <summary>Reads the record.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The record.</returns>
    /// <exception cref="InvalidDataException">The JSON is malformed or a required member is missing.</exception>
    public static Stage4Manifest Parse(string json)
    {
        Argument.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new Stage4Manifest(
                Text(root, "suite"),
                [.. Items(root, "cases").Select(e => Text(e))],
                [.. Items(root, "ownSchemas").Select(ReadSchema)],
                [
                    .. Items(root, "knownFailures")
                        .Select(e => new KnownFailureLink(Text(e, "id"), Number(e, "issue"), Text(e, "reason"))),
                ]
            );
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The stage 4 record is not valid JSON: " + exception.Message, exception);
        }
    }

    private static Stage4Schema ReadSchema(JsonElement schema) =>
        new(
            Text(schema, "schema"),
            [
                .. Items(schema, "documents")
                    .Select(d => new Stage4Document(
                        Text(d, "document"),
                        Flag(d, "valid"),
                        [.. Items(d, "rules").Select(e => Text(e))],
                        d.TryGetProperty("parameters", out var parameters)
                            ? parameters
                                .EnumerateObject()
                                .ToDictionary(p => p.Name, p => Text(p.Value), StringComparer.Ordinal)
                            : new Dictionary<string, string>(StringComparer.Ordinal)
                    )),
            ]
        );

    private static JsonElement.ArrayEnumerator Items(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : throw new InvalidDataException($"The member '{name}' is missing or not an array.");

    private static string Text(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value)
            ? Text(value)
            : throw new InvalidDataException($"The member '{name}' is missing.");

    private static string Text(JsonElement value) =>
        value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!
            : throw new InvalidDataException("A value is not a non-empty string.");

    private static int Number(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var number)
            ? number
            : throw new InvalidDataException($"The member '{name}' is missing or not a number.");

    private static bool Flag(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new InvalidDataException($"The member '{name}' is missing or not a boolean.");
}
