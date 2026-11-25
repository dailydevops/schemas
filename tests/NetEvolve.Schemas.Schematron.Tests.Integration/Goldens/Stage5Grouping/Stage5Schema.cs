namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage5Grouping;

using NetEvolve.Arguments;

/// <summary>One own test schema with its documents.</summary>
internal sealed record Stage5Schema
{
    /// <summary>Initializes a new instance of the <see cref="Stage5Schema"/> class.</summary>
    /// <param name="path">The repository relative path of the schema.</param>
    /// <param name="documents">The test documents.</param>
    public Stage5Schema(string path, IReadOnlyList<Stage5Document> documents)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(documents);
        Path = path;
        Documents = documents;
    }

    /// <summary>Gets the repository relative path of the schema.</summary>
    public string Path { get; init; }

    /// <summary>Gets the test documents.</summary>
    public IReadOnlyList<Stage5Document> Documents { get; init; }
}
