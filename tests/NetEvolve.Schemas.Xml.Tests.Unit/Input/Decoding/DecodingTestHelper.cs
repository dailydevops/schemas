namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Decoding;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xml.Buffers;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Input.Decoding;

internal static class DecodingTestHelper
{
    internal static byte[] Hex(string hex)
    {
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new byte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            bytes[i] = byte.Parse(parts[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    internal static XmlEncodingDetection Detect(byte[] bytes)
    {
        if (!XmlEncodingDetector.TryDetect(bytes, true, out var detection))
        {
            throw new InvalidOperationException("Detection needs more data.");
        }

        return detection;
    }

    /// <summary>Detects on the whole input, then decodes it in the given chunks (split offsets). Returns the chars and the error, if any.</summary>
    internal static (string Text, XmlDecodingError? Error) Decode(byte[] bytes, params int[] splits)
    {
        var detection = Detect(bytes);
        if (!XmlInputDecoder.TryCreate(detection, out var decoder, out var createError))
        {
            return (string.Empty, createError);
        }

        using var output = new PooledBuffer<char>(2);
        var start = 0;
        foreach (var split in splits)
        {
            if (!decoder!.TryDecode(bytes.AsSpan(start, split - start), false, output, out var error))
            {
                return (output.WrittenSpan.ToString(), error);
            }

            start = split;
        }

        return decoder!.TryDecode(bytes.AsSpan(start), true, output, out var last)
            ? (output.WrittenSpan.ToString(), null)
            : (output.WrittenSpan.ToString(), last);
    }
}
