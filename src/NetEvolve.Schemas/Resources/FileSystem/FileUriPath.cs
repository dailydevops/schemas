namespace NetEvolve.Schemas.Resources.FileSystem;

using System;
using System.Text;

/// <summary>Splits an absolute <see langword="file"/> URI into decoded path segments and refuses everything that is not a plain local path.</summary>
internal static class FileUriPath
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    /// <summary>Splits <paramref name="uri"/>; a fragment is ignored.</summary>
    /// <param name="uri">The absolute URI.</param>
    /// <param name="segments">The decoded segments after the leading slash.</param>
    /// <returns>
    /// <see langword="false"/> for another scheme, a foreign authority, a query, a bad escape, invalid UTF-8,
    /// a raw non-ASCII character, or a decoded segment that contains a separator or a control character.
    /// </returns>
    internal static bool TryParse(string uri, out string[] segments)
    {
        segments = [];
        if (!uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = uri.AsSpan("file:".Length);
        var hash = rest.IndexOf('#');
        if (hash >= 0)
        {
            rest = rest.Slice(0, hash);
        }

        if (rest.IndexOf('?') >= 0)
        {
            return false;
        }

        if (!TrySkipAuthority(ref rest))
        {
            return false;
        }

        if (rest.IsEmpty || rest[0] != '/')
        {
            return false;
        }

        rest = rest.Slice(1);
        var count = 1;
        for (var i = 0; i < rest.Length; i++)
        {
            if (rest[i] == '/')
            {
                count++;
            }
        }

        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            var next = rest.IndexOf('/');
            var part = next < 0 ? rest : rest.Slice(0, next);
            if (!TryDecode(part, out result[i]!))
            {
                return false;
            }

            rest = next < 0 ? default : rest.Slice(next + 1);
        }

        segments = result;
        return true;
    }

    private static bool TrySkipAuthority(ref ReadOnlySpan<char> rest)
    {
        if (!rest.StartsWith("//".AsSpan(), StringComparison.Ordinal))
        {
            return true;
        }

        rest = rest.Slice(2);
        var slash = rest.IndexOf('/');
        var authority = slash < 0 ? rest : rest.Slice(0, slash);
        rest = slash < 0 ? default : rest.Slice(slash);
        return authority.IsEmpty || authority.Equals("localhost".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Percent-encodes a path segment for use in a URI.</summary>
    /// <param name="segment">The segment.</param>
    /// <returns>The encoded segment.</returns>
    internal static string Encode(string segment)
    {
        var builder = new StringBuilder(segment.Length);
        foreach (var b in StrictUtf8.GetBytes(segment))
        {
            if (
                b
                is (>= (byte)'a' and <= (byte)'z')
                    or (>= (byte)'A' and <= (byte)'Z')
                    or (>= (byte)'0' and <= (byte)'9')
            )
            {
                _ = builder.Append((char)b);
            }
            else if (b is (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~' or (byte)':' or (byte)'@')
            {
                _ = builder.Append((char)b);
            }
            else
            {
                _ = builder.Append('%').Append(b.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    private static bool TryDecode(ReadOnlySpan<char> part, out string? segment)
    {
        segment = null;
        var bytes = new byte[part.Length];
        var length = 0;
        for (var i = 0; i < part.Length; i++)
        {
            var c = part[i];
            if (c == '%')
            {
                if (i + 2 >= part.Length || !Uri.IsHexDigit(part[i + 1]) || !Uri.IsHexDigit(part[i + 2]))
                {
                    return false;
                }

                bytes[length++] = (byte)((Uri.FromHex(part[i + 1]) << 4) | Uri.FromHex(part[i + 2]));
                i += 2;
            }
            else if (c < 0x80)
            {
                bytes[length++] = (byte)c;
            }
            else
            {
                return false;
            }
        }

        try
        {
            segment = StrictUtf8.GetString(bytes, 0, length);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        foreach (var c in segment)
        {
            if (c < 0x20 || c == 0x7F || c is '/' or '\\')
            {
                return false;
            }
        }

        return true;
    }
}
