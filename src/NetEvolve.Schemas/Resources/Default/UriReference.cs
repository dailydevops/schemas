namespace NetEvolve.Schemas.Resources.Default;

using System;
using System.Buffers;
using NetEvolve.Arguments;

/// <summary>Resolves a URI reference against a base URI (RFC 3986 section 5) after the XML 1.0 section 4.2.2 escaping.</summary>
/// <remarks>
/// Deviations (limitations): the authority is checked against the RFC 3986 character set only, not against the host,
/// IP-literal and port grammar; the scheme is kept as written (no case normalization); no <see cref="System.Uri"/> is used,
/// so the result is identical on every target framework.
/// </remarks>
internal static class UriReference
{
    private const int StackLimit = 512;
    private const string HexDigits = "0123456789ABCDEF";

    /// <summary>Resolves <paramref name="reference"/> against <paramref name="baseUri"/>.</summary>
    /// <param name="reference">The reference as written.</param>
    /// <param name="baseUri">The base URI, or <see langword="null"/>.</param>
    /// <returns>The resolution; the failure code is <see cref="CoreDiagnosticCodes.ResourceReferenceInvalid"/> or <see cref="CoreDiagnosticCodes.ResourceBaseUriMissing"/>.</returns>
    internal static UriResolution Resolve(string reference, string? baseUri)
    {
        ArgumentNullException.ThrowIfNull(reference);

        char[]? rentedReference = null;
        char[]? rentedBase = null;
        try
        {
            if (!TryEscape(reference, ref rentedReference, out var escapedReference))
            {
                return Fail(CoreDiagnosticCodes.ResourceReferenceInvalid);
            }

            if (!TryParse(escapedReference, out var r))
            {
                return Fail(CoreDiagnosticCodes.ResourceReferenceInvalid);
            }

            r.RemoveDots = true;
            if (!r.HasScheme)
            {
                if (string.IsNullOrEmpty(baseUri))
                {
                    return Fail(CoreDiagnosticCodes.ResourceBaseUriMissing);
                }

                if (!TryEscape(baseUri!, ref rentedBase, out var escapedBase) || !TryParse(escapedBase, out var b))
                {
                    return Fail(CoreDiagnosticCodes.ResourceReferenceInvalid);
                }

                if (!b.HasScheme)
                {
                    return Fail(CoreDiagnosticCodes.ResourceBaseUriMissing);
                }

                r = Merge(r, b);
            }

            return Compose(r);
        }
        finally
        {
            if (rentedReference is not null)
            {
                ArrayPool<char>.Shared.Return(rentedReference);
            }

            if (rentedBase is not null)
            {
                ArrayPool<char>.Shared.Return(rentedBase);
            }
        }
    }

    private static UriResolution Fail(DiagnosticCode code) => new(null, code);

    /// <summary>RFC 3986 section 5.2.2, strict: turns the relative reference <paramref name="r"/> into the target.</summary>
    private static Parts Merge(Parts r, Parts b)
    {
        r.HasScheme = true;
        r.Scheme = b.Scheme;
        if (r.HasAuthority)
        {
            return r;
        }

        r.HasAuthority = b.HasAuthority;
        r.Authority = b.Authority;
        if (r.Path.IsEmpty)
        {
            r.Path = b.Path;
            r.RemoveDots = false;
            if (!r.HasQuery)
            {
                r.HasQuery = b.HasQuery;
                r.Query = b.Query;
            }
        }
        else if (r.Path[0] != '/')
        {
            // RFC 3986 section 5.2.3: merge.
            r.PathPrefix =
                b.HasAuthority && b.Path.IsEmpty ? "/".AsSpan() : b.Path.Slice(0, b.Path.LastIndexOf('/') + 1);
        }

        return r;
    }

    private static UriResolution Compose(Parts target)
    {
        var capacity =
            target.Scheme.Length
            + target.Authority.Length
            + target.PathPrefix.Length
            + target.Path.Length
            + target.Query.Length
            + target.Fragment.Length
            + 5;

        if (capacity <= StackLimit)
        {
            Span<char> stack = stackalloc char[StackLimit];
            return Write(stack, target);
        }

        var rented = ArrayPool<char>.Shared.Rent(capacity);
        try
        {
            return Write(rented, target);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static UriResolution Write(Span<char> buffer, Parts target)
    {
        target.Scheme.CopyTo(buffer);
        var length = target.Scheme.Length;
        buffer[length++] = ':';

        if (target.HasAuthority)
        {
            buffer[length++] = '/';
            buffer[length++] = '/';
            target.Authority.CopyTo(buffer.Slice(length));
            length += target.Authority.Length;
        }

        var pathStart = length;
        target.PathPrefix.CopyTo(buffer.Slice(length));
        length += target.PathPrefix.Length;
        target.Path.CopyTo(buffer.Slice(length));
        length += target.Path.Length;
        if (target.RemoveDots)
        {
            length = pathStart + RemoveDotSegments(buffer.Slice(pathStart, length - pathStart));
        }

        if (target.HasQuery)
        {
            buffer[length++] = '?';
            target.Query.CopyTo(buffer.Slice(length));
            length += target.Query.Length;
        }

        if (target.HasFragment)
        {
            buffer[length++] = '#';
            target.Fragment.CopyTo(buffer.Slice(length));
            length += target.Fragment.Length;
        }

        return new UriResolution(buffer.Slice(0, length).ToString(), default);
    }

    /// <summary>RFC 3986 section 5.2.4, in place; the output never outruns the input.</summary>
    private static int RemoveDotSegments(Span<char> path)
    {
        var read = 0;
        var write = 0;
        while (read < path.Length)
        {
            ReadOnlySpan<char> rest = path.Slice(read);
            if (rest.StartsWith("../".AsSpan(), StringComparison.Ordinal))
            {
                read += 3;
            }
            else if (rest.StartsWith("./".AsSpan(), StringComparison.Ordinal))
            {
                read += 2;
            }
            else if (rest.StartsWith("/./".AsSpan(), StringComparison.Ordinal))
            {
                read += 2;
            }
            else if (rest.Equals("/.".AsSpan(), StringComparison.Ordinal))
            {
                path[write++] = '/';
                break;
            }
            else if (rest.StartsWith("/../".AsSpan(), StringComparison.Ordinal))
            {
                read += 3;
                write = LastSegmentStart(path, write);
            }
            else if (rest.Equals("/..".AsSpan(), StringComparison.Ordinal))
            {
                write = LastSegmentStart(path, write);
                path[write++] = '/';
                break;
            }
            else if (
                rest.Equals(".".AsSpan(), StringComparison.Ordinal)
                || rest.Equals("..".AsSpan(), StringComparison.Ordinal)
            )
            {
                break;
            }
            else
            {
                var next = rest.Slice(1).IndexOf('/');
                var count = next < 0 ? rest.Length : next + 1;
                rest.Slice(0, count).CopyTo(path.Slice(write));
                write += count;
                read += count;
            }
        }

        return write;
    }

    private static int LastSegmentStart(ReadOnlySpan<char> path, int length)
    {
        var index = path.Slice(0, length).LastIndexOf('/');
        return index < 0 ? 0 : index;
    }

    /// <summary>XML 1.0 section 4.2.2 escaping; <see langword="false"/> for a lone surrogate or a bad percent escape.</summary>
    private static bool TryEscape(string value, ref char[]? rented, out ReadOnlySpan<char> escaped)
    {
        var length = Escape(value, default, false);
        if (length < 0)
        {
            escaped = default;
            return false;
        }

        if (length == value.Length)
        {
            escaped = value.AsSpan();
            return true;
        }

        rented = ArrayPool<char>.Shared.Rent(length);
        _ = Escape(value, rented, true);
        escaped = rented.AsSpan(0, length);
        return true;
    }

    private static int Escape(ReadOnlySpan<char> value, Span<char> destination, bool write)
    {
        var length = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '%')
            {
                if (i + 2 >= value.Length || !IsHex(value[i + 1]) || !IsHex(value[i + 2]))
                {
                    return -1;
                }

                if (write)
                {
                    value.Slice(i, 3).CopyTo(destination.Slice(length));
                }

                length += 3;
                i += 2;
            }
            else if (c < 0x80 && !NeedsEscape(c))
            {
                if (write)
                {
                    destination[length] = c;
                }

                length++;
            }
            else
            {
                int codePoint = c;
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    {
                        return -1;
                    }

                    codePoint = char.ConvertToUtf32(c, value[++i]);
                }
                else if (char.IsLowSurrogate(c))
                {
                    return -1;
                }

                length += EscapeCodePoint(codePoint, destination, length, write);
            }
        }

        return length;
    }

    private static int EscapeCodePoint(int codePoint, Span<char> destination, int offset, bool write)
    {
        Span<byte> bytes = stackalloc byte[4];
        int count;
        if (codePoint < 0x80)
        {
            bytes[0] = (byte)codePoint;
            count = 1;
        }
        else if (codePoint < 0x800)
        {
            bytes[0] = (byte)(0xC0 | (codePoint >> 6));
            bytes[1] = (byte)(0x80 | (codePoint & 0x3F));
            count = 2;
        }
        else if (codePoint < 0x10000)
        {
            bytes[0] = (byte)(0xE0 | (codePoint >> 12));
            bytes[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            bytes[2] = (byte)(0x80 | (codePoint & 0x3F));
            count = 3;
        }
        else
        {
            bytes[0] = (byte)(0xF0 | (codePoint >> 18));
            bytes[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
            bytes[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            bytes[3] = (byte)(0x80 | (codePoint & 0x3F));
            count = 4;
        }

        if (write)
        {
            for (var i = 0; i < count; i++)
            {
                destination[offset + (i * 3)] = '%';
                destination[offset + (i * 3) + 1] = HexDigits[bytes[i] >> 4];
                destination[offset + (i * 3) + 2] = HexDigits[bytes[i] & 0xF];
            }
        }

        return count * 3;
    }

    private static bool NeedsEscape(char c) =>
        c <= 0x20 || c == 0x7F || c is '<' or '>' or '"' or '{' or '}' or '|' or '\\' or '^' or '`';

    private static bool IsHex(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static bool IsAlpha(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    private static bool IsUnreservedOrSubDelim(char c) =>
        IsAlpha(c)
        || c is (>= '0' and <= '9')
        || c is '-' or '.' or '_' or '~' or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=';

    /// <summary>RFC 3986 appendix B split with the character-set validation of every component.</summary>
    private static bool TryParse(ReadOnlySpan<char> value, out Parts parts)
    {
        parts = default;
        var rest = value;

        var hash = rest.IndexOf('#');
        if (hash >= 0)
        {
            parts.HasFragment = true;
            parts.Fragment = rest.Slice(hash + 1);
            rest = rest.Slice(0, hash);
            if (!IsValid(parts.Fragment, false, true))
            {
                return false;
            }
        }

        var question = rest.IndexOf('?');
        if (question >= 0)
        {
            parts.HasQuery = true;
            parts.Query = rest.Slice(question + 1);
            rest = rest.Slice(0, question);
            if (!IsValid(parts.Query, false, true))
            {
                return false;
            }
        }

        var colon = rest.IndexOf(':');
        if (colon >= 0 && rest.Slice(0, colon).IndexOf('/') < 0)
        {
            // Without a slash before the colon, the prefix must be a scheme (a relative path may not start with "x:").
            var scheme = rest.Slice(0, colon);
            if (!IsScheme(scheme))
            {
                return false;
            }

            parts.HasScheme = true;
            parts.Scheme = scheme;
            rest = rest.Slice(colon + 1);
        }

        if (rest.Length >= 2 && rest[0] == '/' && rest[1] == '/')
        {
            rest = rest.Slice(2);
            var slash = rest.IndexOf('/');
            parts.HasAuthority = true;
            parts.Authority = slash < 0 ? rest : rest.Slice(0, slash);
            rest = slash < 0 ? default : rest.Slice(slash);
            if (!IsValid(parts.Authority, true, false))
            {
                return false;
            }
        }

        parts.Path = rest;
        return IsValid(rest, false, false);
    }

    private static bool IsScheme(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || !IsAlpha(value[0]))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!IsAlpha(c) && !(c is (>= '0' and <= '9') or '+' or '-' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValid(ReadOnlySpan<char> value, bool allowBrackets, bool allowQuestion)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (
                !IsUnreservedOrSubDelim(c)
                && c is not (':' or '@' or '/' or '%')
                && !(allowBrackets && c is '[' or ']')
                && !(allowQuestion && c == '?')
            )
            {
                return false;
            }
        }

        return true;
    }

    private ref struct Parts
    {
        internal ReadOnlySpan<char> PathPrefix;
        internal bool RemoveDots;
        internal ReadOnlySpan<char> Scheme;
        internal bool HasScheme;
        internal ReadOnlySpan<char> Authority;
        internal bool HasAuthority;
        internal ReadOnlySpan<char> Path;
        internal ReadOnlySpan<char> Query;
        internal bool HasQuery;
        internal ReadOnlySpan<char> Fragment;
        internal bool HasFragment;
    }
}
