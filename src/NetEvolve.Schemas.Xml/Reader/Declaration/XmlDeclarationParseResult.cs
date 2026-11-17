namespace NetEvolve.Schemas.Xml.Reader.Declaration;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of <see cref="XmlDeclarationParser.Parse"/>: absent, valid with the pseudo-attribute values, or invalid with a code.</summary>
/// <remarks>
/// The value spans point into the parsed input. Positions carry the zero-based offset of the first value character
/// (after the opening quote); line and column are zero (unknown), the reader owns line tracking.
/// </remarks>
internal readonly ref struct XmlDeclarationParseResult
{
    private XmlDeclarationParseResult(
        bool isPresent,
        XmlErrorCode? error,
        int errorOffset,
        int length,
        ReadOnlySpan<char> version,
        ReadOnlySpan<char> encoding,
        bool? standalone,
        XmlPosition? versionPosition,
        XmlPosition? encodingPosition,
        XmlPosition? standalonePosition
    )
    {
        IsPresent = isPresent;
        Error = error;
        ErrorOffset = errorOffset;
        Length = length;
        Version = version;
        Encoding = encoding;
        Standalone = standalone;
        VersionPosition = versionPosition;
        EncodingPosition = encodingPosition;
        StandalonePosition = standalonePosition;
    }

    /// <summary>Gets a value indicating whether the input starts with a declaration (valid or not).</summary>
    public bool IsPresent { get; }

    /// <summary>Gets the error code, <see langword="null"/> when the declaration is absent or valid.</summary>
    public XmlErrorCode? Error { get; }

    /// <summary>Gets the zero-based offset of the error in the input.</summary>
    public int ErrorOffset { get; }

    /// <summary>Gets the number of characters of the declaration, from <c>&lt;?xml</c> through <c>?&gt;</c>.</summary>
    public int Length { get; }

    /// <summary>Gets the version value, empty when absent.</summary>
    public ReadOnlySpan<char> Version { get; }

    /// <summary>Gets the encoding value, empty when absent.</summary>
    public ReadOnlySpan<char> Encoding { get; }

    /// <summary>Gets the standalone value, <see langword="null"/> when absent.</summary>
    public bool? Standalone { get; }

    /// <summary>Gets the position of the version value, <see langword="null"/> when absent.</summary>
    public XmlPosition? VersionPosition { get; }

    /// <summary>Gets the position of the encoding value, <see langword="null"/> when absent.</summary>
    public XmlPosition? EncodingPosition { get; }

    /// <summary>Gets the position of the standalone value, <see langword="null"/> when absent.</summary>
    public XmlPosition? StandalonePosition { get; }

    /// <summary>Gets a value indicating whether a declaration is present and well formed.</summary>
    public bool IsValid => IsPresent && Error is null;

    internal static XmlDeclarationParseResult Absent => default;

    internal static XmlDeclarationParseResult Invalid(XmlErrorCode error, int offset) =>
        new(true, error, offset, 0, default, default, null, null, null, null);

    internal static XmlDeclarationParseResult Valid(
        int length,
        ReadOnlySpan<char> version,
        ReadOnlySpan<char> encoding,
        bool? standalone,
        XmlPosition? versionPosition,
        XmlPosition? encodingPosition,
        XmlPosition? standalonePosition
    ) =>
        new(
            true,
            null,
            0,
            length,
            version,
            encoding,
            standalone,
            versionPosition,
            encodingPosition,
            standalonePosition
        );
}
