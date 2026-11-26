namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.FrenchCtc;

using System.Text;

/// <summary>A temporary directory that is deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Root { get; } =
        Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "french-ctc-" + Guid.NewGuid().ToString("N")))
            .FullName;

    public void Write(string relativePath, string content) =>
        File.WriteAllText(Path.Combine(Root, relativePath), content, new UTF8Encoding(false));

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
