#pragma warning disable CA1815 // Approved shape (#529): the result defines no value equality.

namespace NetEvolve.Schemas.Formats;

using System;

/// <summary>The result of <see cref="ResourceLoader.Load"/>: the content, or the code and message of the failure.</summary>
/// <remarks>The result carries data only; each format turns it into its own issue type.</remarks>
public readonly struct ResourceLoadResult
{
    private readonly string? _message;

    internal ResourceLoadResult(
        ResourceLoadStatus status,
        ReadOnlyMemory<byte> content,
        string? systemId,
        DiagnosticCode code,
        string? message
    )
    {
        Status = status;
        Content = content;
        SystemId = systemId;
        Code = code;
        _message = message;
    }

    /// <summary>Gets the outcome.</summary>
    public ResourceLoadStatus Status { get; }

    /// <summary>Gets the bytes of the resource; empty unless <see cref="Status"/> is <see cref="ResourceLoadStatus.Loaded"/>.</summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Gets the system identifier of the resource (after redirects, else the resolved URI); <see langword="null"/> unless it was loaded or exceeded the size limit.</summary>
    public string? SystemId { get; }

    /// <summary>Gets the diagnostic code of the failure; the default code when the resource was loaded.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the message of the failure, the resolver's when it gave one; empty when the resource was loaded.</summary>
    public string Message => _message ?? string.Empty;
}
