namespace NetEvolve.Schemas.Resources.FileSystem;

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources.Default;

/// <summary>Serves resources from one root directory and refuses everything outside it.</summary>
/// <remarks>
/// <para>
/// The reference is resolved against the base URI (the root directory when there is none) and must end up as a
/// <see langword="file"/> URI without query and with an empty or <c>localhost</c> authority, below the root. Every decoded path
/// segment must be a plain name: no <c>.</c>, <c>..</c>, empty segment, separator, control character or colon.
/// A symbolic link or junction below the root is served only when its final target is below the root as well.
/// </para>
/// <para>
/// Limitations: the file system can change between the check and the open (time of check to time of use); case
/// sensitivity follows the host (ordinal-ignore-case on Windows, ordinal elsewhere); a name with a colon is refused on
/// every platform; a reparse point whose target cannot be determined, and every link on a runtime without the link
/// target API, is refused; the root directory itself is trusted and never checked for links.
/// </para>
/// </remarks>
public sealed class SandboxedFileSystemResourceResolver : IResourceResolver
{
#if !NET6_0_OR_GREATER
    private static readonly MethodInfo? ResolveLinkTargetMethod = typeof(FileSystemInfo).GetMethod(
        "ResolveLinkTarget",
        [typeof(bool)]
    );
#endif
    private static readonly bool IsWindows = Path.DirectorySeparatorChar == '\\';
    private static readonly StringComparison Comparison = IsWindows
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private readonly string _root;
    private readonly string _rootPrefix;
    private readonly string _rootUri;
    private readonly string[] _rootSegments;

    /// <summary>Initializes a new instance of the <see cref="SandboxedFileSystemResourceResolver"/> class.</summary>
    /// <param name="rootDirectory">The root directory; relative paths are resolved against the current directory once.</param>
    public SandboxedFileSystemResourceResolver(string rootDirectory)
    {
        Argument.ThrowIfNullOrWhiteSpace(rootDirectory);

        _root = Path.GetFullPath(rootDirectory);
        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        _rootPrefix = _root.TrimEnd(separators) + Path.DirectorySeparatorChar;

        var uniform = IsWindows ? _root.Replace('\\', '/') : _root;
        var trimmed = uniform.TrimEnd('/');
        _rootSegments = trimmed.Length == 0 ? [] : trimmed.Substring(trimmed[0] == '/' ? 1 : 0).Split('/');

        var uri = new StringBuilder("file:///");
        foreach (var segment in _rootSegments)
        {
            _ = uri.Append(FileUriPath.Encode(segment)).Append('/');
        }

        _rootUri = uri.ToString();
    }

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uri = request.ResolvedUri ?? UriReference.Resolve(request.Reference, BaseOrRoot(in request)).Uri;
        if (uri is null)
        {
            return Deny(in request, "the reference is invalid");
        }

        if (!FileUriPath.TryParse(uri, out var segments))
        {
            return Deny(in request, "only plain local file URIs are served");
        }

        var reason = TryMap(segments, out var path, out var exists);
        if (reason is not null)
        {
            return Deny(in request, reason);
        }

        var full = Path.GetFullPath(path);
        if (full.Length <= _rootPrefix.Length || !full.StartsWith(_rootPrefix, Comparison))
        {
            return Deny(in request, "the path is not below the root directory");
        }

        var hash = uri.AsSpan().IndexOf('#');
        var systemId = hash < 0 ? uri : uri.Substring(0, hash);
        if (!exists || !File.Exists(full))
        {
            return ResourceResult.NotFound(
                $"{CoreDiagnosticCodes.ResourceNotFound}: {request.Kind} resource '{systemId}' does not exist inside the root directory."
            );
        }

        try
        {
            return ResourceResult.Found(
                new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan),
                systemId
            );
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Deny(in request, "the file could not be opened");
        }
    }

    private string BaseOrRoot(in ResourceRequest request) =>
        string.IsNullOrEmpty(request.BaseUri) ? _rootUri : request.BaseUri!;

    /// <summary>Maps the segments below the root to a path; returns the refusal reason, or <see langword="null"/>.</summary>
    private string? TryMap(string[] segments, out string path, out bool exists)
    {
        path = _root;
        exists = true;
        if (segments.Length <= _rootSegments.Length)
        {
            return "the URI is not below the root directory";
        }

        for (var i = 0; i < _rootSegments.Length; i++)
        {
            if (!string.Equals(segments[i], _rootSegments[i], Comparison))
            {
                return "the URI is not below the root directory";
            }
        }

        for (var i = _rootSegments.Length; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0 || segment is "." or ".." || segment.AsSpan().IndexOf(':') >= 0)
            {
                return "a path segment is not a plain name";
            }

            path = Path.Combine(path, segment);
            var state = exists ? Probe(path) : PathState.Missing;
            if (state == PathState.Escapes)
            {
                return "a link leaves the root directory";
            }

            exists = state == PathState.Inside;
        }

        return null;
    }

    private static ResourceResult Deny(in ResourceRequest request, string reason) =>
        ResourceResult.Denied(
            $"{CoreDiagnosticCodes.ResourceDenied}: {request.Kind} resource '{request.ResolvedUri ?? request.Reference}' was denied: {reason} (base URI '{request.BaseUri ?? "(none)"}')."
        );

    private PathState Probe(string path)
    {
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        if (!info.Exists)
        {
            return PathState.Missing;
        }

        if ((info.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
        {
            return PathState.Inside;
        }

        var target = LinkTarget(info);
        return target is not null && Path.GetFullPath(target).StartsWith(_rootPrefix, Comparison)
            ? PathState.Inside
            : PathState.Escapes;
    }

    private static string? LinkTarget(FileSystemInfo info)
    {
        try
        {
#if NET6_0_OR_GREATER
            return info.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
#else
            return (ResolveLinkTargetMethod?.Invoke(info, [true]) as FileSystemInfo)?.FullName;
#endif
        }
        catch (Exception e) when (e is IOException or TargetInvocationException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private enum PathState
    {
        Missing,
        Inside,
        Escapes,
    }
}
