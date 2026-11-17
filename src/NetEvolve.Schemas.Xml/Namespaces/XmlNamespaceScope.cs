namespace NetEvolve.Schemas.Xml.Namespaces;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Skeleton, implemented in the next commit.</summary>
internal sealed class XmlNamespaceScope
{
    internal XmlNamespaceScope(XmlNameTable names) => throw new NotSupportedException();

    internal int Depth => throw new NotSupportedException();

    internal int InScopeCount => throw new NotSupportedException();

    internal static bool IsDeclaration(ReadOnlySpan<char> attributeName) => throw new NotSupportedException();

    internal void PushElement() => throw new NotSupportedException();

    internal XmlErrorCode? Declare(
        ReadOnlySpan<char> attributeName,
        ReadOnlySpan<char> value,
        out XmlNamespaceBinding binding
    ) => throw new NotSupportedException();

    internal XmlErrorCode? ResolveElement(ReadOnlySpan<char> qualifiedName, out XmlName name) =>
        throw new NotSupportedException();

    internal XmlErrorCode? ResolveAttribute(ReadOnlySpan<char> qualifiedName, out XmlName name) =>
        throw new NotSupportedException();

    internal XmlNamespaceBinding GetInScope(int index) => throw new NotSupportedException();

    internal void PopElement() => throw new NotSupportedException();
}
