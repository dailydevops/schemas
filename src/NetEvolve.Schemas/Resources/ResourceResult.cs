#pragma warning disable CA1815 // Approved shape (#103): the struct defines no value equality.

namespace NetEvolve.Schemas.Resources;

using System;
using System.IO;
using NetEvolve.Arguments;

/// <summary>The outcome of resolving a resource. <see langword="default"/> denies.</summary>
public readonly struct ResourceResult : IDisposable
{
    private ResourceResult(
        ResourceStatus status,
        Stream? stream,
        ReadOnlyMemory<byte> content,
        string? systemId,
        string? message
    )
    {
        Status = status;
        Stream = stream;
        Content = content;
        SystemId = systemId;
        Message = message;
    }

    /// <summary>Gets the status.</summary>
    public ResourceStatus Status { get; }

    /// <summary>Gets the stream of a <c>Found(Stream)</c> result; ownership passes to the library.</summary>
    public Stream? Stream { get; }

    /// <summary>Gets the content of a <c>Found(ReadOnlyMemory&lt;byte&gt;)</c> result.</summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Gets the final URI after redirects, the base URI of the loaded resource.</summary>
    public string? SystemId { get; }

    /// <summary>Gets the message of a denial or miss.</summary>
    public string? Message { get; }

    /// <summary>Creates a found result over a stream. Ownership passes to the library.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="systemId">The final URI, or <see langword="null"/>.</param>
    /// <returns>The result.</returns>
    public static ResourceResult Found(Stream stream, string? systemId = null)
    {
        Argument.ThrowIfNull(stream);

        return new(ResourceStatus.Found, stream, default, systemId, message: null);
    }

    /// <summary>Creates a found result over memory. The library takes no ownership.</summary>
    /// <param name="content">The content.</param>
    /// <param name="systemId">The final URI, or <see langword="null"/>.</param>
    /// <returns>The result.</returns>
    public static ResourceResult Found(ReadOnlyMemory<byte> content, string? systemId = null) =>
        new(ResourceStatus.Found, stream: null, content, systemId, message: null);

    /// <summary>Creates a result that denies the resource.</summary>
    /// <param name="message">The reason, or <see langword="null"/>.</param>
    /// <returns>The result.</returns>
    public static ResourceResult Denied(string? message = null) =>
        new(ResourceStatus.Denied, stream: null, default, systemId: null, message);

    /// <summary>Creates a result for a resource that does not exist.</summary>
    /// <param name="message">The reason, or <see langword="null"/>.</param>
    /// <returns>The result.</returns>
    public static ResourceResult NotFound(string? message = null) =>
        new(ResourceStatus.NotFound, stream: null, default, systemId: null, message);

    /// <inheritdoc />
    /// <remarks>Disposes <see cref="Stream"/> when present; repeated calls and calls on copies are harmless.</remarks>
    public void Dispose() => Stream?.Dispose();
}
