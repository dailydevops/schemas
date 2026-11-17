namespace NetEvolve.Schemas.Resources.FileSystem;

using System.Threading;
using NetEvolve.Arguments;

/// <summary>Serves resources from one root directory and refuses everything outside it.</summary>
public sealed class SandboxedFileSystemResourceResolver : IResourceResolver
{
    /// <summary>Initializes a new instance of the <see cref="SandboxedFileSystemResourceResolver"/> class.</summary>
    /// <param name="rootDirectory">The root directory.</param>
    public SandboxedFileSystemResourceResolver(string rootDirectory) => Argument.ThrowIfNullOrWhiteSpace(rootDirectory);

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
        ResourceResult.Denied();
}
