namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() =>
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "differential-" + Guid.NewGuid().ToString("N"));

    public string Path { get; }

    /// <summary>Writes a file below the directory, creating the directory when needed.</summary>
    /// <param name="relativePath">The path relative to the directory.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The absolute path of the file.</returns>
    public string Write(string relativePath, string content)
    {
        var path = System.IO.Path.Combine(Path, relativePath);
        _ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
