namespace NetEvolve.Schemas.Xml.Dtd.External.Loader;

using System;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;

internal sealed class ExternalEntityFrame : IDisposable
{
    internal ResourceKind Kind => throw new NotSupportedException();

    internal string Name => throw new NotSupportedException();

    internal string? ResolvedUri => throw new NotSupportedException();

    internal string? BaseUri => throw new NotSupportedException();

    internal XmlErrorCode? Error => throw new NotSupportedException();

    internal long ErrorByteOffset => throw new NotSupportedException();

    internal bool TryRead(out ReadOnlyMemory<char> chunk) => throw new NotSupportedException();

    public void Dispose() => throw new NotSupportedException();
}
