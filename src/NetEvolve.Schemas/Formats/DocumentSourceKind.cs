#pragma warning disable CA1028 // Approved shape (#103): byte is the fixed underlying enum type.

namespace NetEvolve.Schemas.Formats;

/// <summary>Tells a format core whether it must detect the encoding.</summary>
public enum DocumentSourceKind : byte
{
    /// <summary>Encoded bytes; the format detects the encoding.</summary>
    Bytes = 0,

    /// <summary>Decoded characters.</summary>
    Chars = 1,
}
