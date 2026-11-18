namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Harness;

using NetEvolve.Arguments;

/// <summary>A golden document that was fetched through its pinned suite.</summary>
internal sealed record GoldenDocument
{
    /// <summary>Initializes a new instance of the <see cref="GoldenDocument"/> class.</summary>
    /// <param name="id">The case id of the source.</param>
    /// <param name="path">The full path of the extracted document.</param>
    public GoldenDocument(string id, string path)
    {
        Argument.ThrowIfNullOrWhiteSpace(id);
        Argument.ThrowIfNullOrWhiteSpace(path);
        Id = id;
        Path = path;
    }

    /// <summary>Gets the case id of the source.</summary>
    public string Id { get; init; }

    /// <summary>Gets the full path of the extracted document.</summary>
    public string Path { get; init; }
}
