#pragma warning disable CA1028 // The approved contract fixes byte as the underlying type.

namespace NetEvolve.Schemas.Formats;

/// <summary>Tells a format core whether it must detect the encoding.</summary>
public enum DocumentSourceKind : byte
{
    /// <summary>Encoded bytes; the format detects the encoding.</summary>
    Bytes = 0,

    /// <summary>Decoded characters.</summary>
    Chars = 1,
}
