namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using System;
using System.Text;

/// <summary>Decodes the bytes of an <c>xi:include</c> element with <c>parse="text"</c>.</summary>
/// <remarks>
/// Limitation: the encodings are a fixed set, UTF-8, UTF-16 (with and without byte order), US-ASCII, and ISO-8859-1, so every
/// target framework decodes alike; another name is <c>SCH-UNSUPPORTED</c>. A byte sequence the encoding forbids and a character
/// XML does not allow are <c>SCH-MODULE-INVALID</c>. Line ends are kept as they are.
/// </remarks>
internal static class XIncludeText
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly Encoding Utf16Little = new UnicodeEncoding(false, false, true);
    private static readonly Encoding Utf16Big = new UnicodeEncoding(true, false, true);
    private static readonly Encoding Ascii = Encoding.GetEncoding(
        "us-ascii",
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback
    );
    private static readonly Encoding Latin1 = Encoding.GetEncoding(
        "iso-8859-1",
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback
    );

    /// <summary>Decodes the content.</summary>
    /// <param name="content">The bytes of the resource.</param>
    /// <param name="encoding">The value of the <c>encoding</c> attribute, or <see langword="null"/> to detect it.</param>
    /// <param name="text">The text.</param>
    /// <param name="code">The failure code when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when the content is decoded and holds only characters XML allows.</returns>
    internal static bool TryDecode(
        ReadOnlyMemory<byte> content,
        string? encoding,
        out string text,
        out DiagnosticCode code
    )
    {
        var bytes = content.ToArray();
        text = string.Empty;
        code = default;
        if (!TrySelect(bytes, encoding, out var selected, out var skip))
        {
            code = SchematronDiagnosticCodes.Unsupported;
            return false;
        }

        try
        {
            text = selected.GetString(bytes, skip, bytes.Length - skip);
        }
        catch (DecoderFallbackException)
        {
            code = SchematronDiagnosticCodes.ModuleInvalid;
            return false;
        }

        foreach (var c in text)
        {
            if (c < 0x20 && c != '\t' && c != '\n' && c != '\r' || c == 0xFFFE || c == 0xFFFF)
            {
                text = string.Empty;
                code = SchematronDiagnosticCodes.ModuleInvalid;
                return false;
            }
        }

        return true;
    }

    private static bool TrySelect(byte[] bytes, string? name, out Encoding encoding, out int skip)
    {
        skip = 0;
        var utf8Bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var little = bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE;
        var big = bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF;
        switch (name?.ToUpperInvariant())
        {
            case null:
                encoding =
                    little ? Utf16Little
                    : big ? Utf16Big
                    : Utf8;
                skip =
                    little || big ? 2
                    : utf8Bom ? 3
                    : 0;
                return true;
            case "UTF-8":
                encoding = Utf8;
                skip = utf8Bom ? 3 : 0;
                return true;
            case "UTF-16":
                encoding = little ? Utf16Little : Utf16Big;
                skip = little || big ? 2 : 0;
                return true;
            case "UTF-16LE":
                encoding = Utf16Little;
                return true;
            case "UTF-16BE":
                encoding = Utf16Big;
                return true;
            case "US-ASCII":
                encoding = Ascii;
                return true;
            case "ISO-8859-1":
                encoding = Latin1;
                return true;
            default:
                encoding = Utf8;
                return false;
        }
    }
}
