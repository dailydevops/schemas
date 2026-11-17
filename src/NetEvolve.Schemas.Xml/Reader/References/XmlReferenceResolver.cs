namespace NetEvolve.Schemas.Xml.Reader.References;

using System;
using NetEvolve.Schemas.Xml.Contracts;

internal static class XmlReferenceResolver
{
    internal static XmlErrorCode? TryParseReference(ReadOnlySpan<char> text, out int length, out int codePoint) =>
        throw new NotSupportedException();

    internal static XmlErrorCode? TryResolve(
        ReadOnlySpan<char> text,
        Span<char> buffer,
        out ReadOnlySpan<char> resolved,
        out int errorOffset
    ) => throw new NotSupportedException();
}
