namespace NetEvolve.Schemas.Conformance.Tests.Unit.Fetch;

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.Conformance.Manifest;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() =>
        Path = Directory
            .CreateDirectory(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "conformance-" + Guid.NewGuid().ToString("N"))
            )
            .FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
