namespace NetEvolve.Schemas.Xml.Reader.Core;

using System;
using System.Runtime.InteropServices;

/// <summary>A lexical token: slices of the span passed to the producing call, or at most two inline characters.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XmlToken
{
    internal XmlToken(XmlTokenKind kind, int start, int length, int start2 = 0, int length2 = 0, char char0 = '\0')
    {
        Kind = kind;
        Start = start;
        Length = length;
        Start2 = start2;
        Length2 = length2;
        Char0 = char0;
        Char1 = '\0';
        InlineCount = 0;
    }

    private XmlToken(XmlTokenKind kind, char char0, char char1, int inlineCount)
    {
        Kind = kind;
        Start = 0;
        Length = 0;
        Start2 = 0;
        Length2 = 0;
        Char0 = char0;
        Char1 = char1;
        InlineCount = inlineCount;
    }

    internal XmlTokenKind Kind { get; }

    /// <summary>Gets the offset of the first slice in the span of the producing call.</summary>
    internal int Start { get; }

    internal int Length { get; }

    /// <summary>Gets the offset of the second slice.</summary>
    internal int Start2 { get; }

    internal int Length2 { get; }

    internal char Char0 { get; }

    internal char Char1 { get; }

    /// <summary>Gets the number of inline characters, zero for a slice token.</summary>
    internal int InlineCount { get; }

    internal static XmlToken Inline(XmlTokenKind kind, char char0) => new(kind, char0, '\0', 1);

    internal static XmlToken InlineCodePoint(XmlTokenKind kind, int codePoint) =>
        codePoint <= 0xFFFF
            ? new XmlToken(kind, (char)codePoint, '\0', 1)
            : new XmlToken(
                kind,
                (char)(0xD800 + ((codePoint - 0x10000) >> 10)),
                (char)(0xDC00 + ((codePoint - 0x10000) & 0x3FF)),
                2
            );

    internal ReadOnlySpan<char> First(ReadOnlySpan<char> input) => input.Slice(Start, Length);

    internal ReadOnlySpan<char> Second(ReadOnlySpan<char> input) => input.Slice(Start2, Length2);
}
