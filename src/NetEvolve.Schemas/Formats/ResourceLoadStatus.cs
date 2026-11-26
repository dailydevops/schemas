#pragma warning disable CA1028 // Approved shape (#529): byte is the fixed underlying enum type.

namespace NetEvolve.Schemas.Formats;

/// <summary>The outcome of <see cref="ResourceLoader.Load"/>.</summary>
public enum ResourceLoadStatus : byte
{
    /// <summary>The resource was read; <see cref="ResourceLoadResult.Content"/> is set.</summary>
    Loaded = 0,

    /// <summary>The reference is invalid, the base is missing, or the resource was denied or not found; the caller may recover.</summary>
    Failed = 1,

    /// <summary><c>MaxExternalResources</c> or <c>MaxResourceSize</c> was exceeded; the caller should stop.</summary>
    LimitExceeded = 2,
}
