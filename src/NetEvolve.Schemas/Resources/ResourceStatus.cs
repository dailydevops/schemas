#pragma warning disable CA1028 // Approved shape (#103): byte is the fixed underlying enum type.

namespace NetEvolve.Schemas.Resources;

/// <summary>The outcome of a resolve call. <see langword="default"/> is <see cref="Denied"/>.</summary>
public enum ResourceStatus : byte
{
    /// <summary>The resolver refused the resource.</summary>
    Denied = 0,

    /// <summary>The resource was found.</summary>
    Found = 1,

    /// <summary>The resolver has no such resource.</summary>
    NotFound = 2,
}
