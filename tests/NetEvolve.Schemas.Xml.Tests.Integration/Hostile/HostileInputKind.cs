namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

/// <summary>The input kinds of document loading that every corpus file runs through.</summary>
internal enum HostileInputKind
{
    /// <summary>A span of characters.</summary>
    CharSpan,

    /// <summary>A span of bytes.</summary>
    ByteSpan,

    /// <summary>Characters in memory.</summary>
    CharMemory,

    /// <summary>Bytes in memory.</summary>
    ByteMemory,

    /// <summary>A multi-segment sequence of characters.</summary>
    CharSequence,

    /// <summary>A multi-segment sequence of bytes.</summary>
    ByteSequence,

    /// <summary>A stream that returns short reads.</summary>
    Stream,
}
