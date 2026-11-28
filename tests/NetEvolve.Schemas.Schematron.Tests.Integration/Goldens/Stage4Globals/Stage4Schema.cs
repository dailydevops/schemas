namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage4Globals;

/// <summary>One own test schema with its documents.</summary>
internal sealed record Stage4Schema
{
    /// <summary>Initializes a new instance of the <see cref="Stage4Schema"/> class.</summary>
    /// <param name="path">The repository relative path of the schema.</param>
    /// <param name="documents">The test documents.</param>
    public Stage4Schema(string path, IReadOnlyList<Stage4Document> documents)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(documents);
        Path = path;
        Documents = documents;
    }

    /// <summary>Gets the repository relative path of the schema.</summary>
    public string Path { get; init; }

    /// <summary>Gets the test documents.</summary>
    public IReadOnlyList<Stage4Document> Documents { get; init; }
}
