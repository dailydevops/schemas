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

    /// <summary>A sequence of characters in one segment.</summary>
    CharSequenceSingle,

    /// <summary>A sequence of bytes in one segment.</summary>
    ByteSequenceSingle,

    /// <summary>A sequence of characters in at least two segments.</summary>
    CharSequenceSegmented,

    /// <summary>A sequence of bytes in at least two segments.</summary>
    ByteSequenceSegmented,

    /// <summary>A stream that returns at most half of the document, or a fixed number of bytes, per read.</summary>
    Stream,
}
