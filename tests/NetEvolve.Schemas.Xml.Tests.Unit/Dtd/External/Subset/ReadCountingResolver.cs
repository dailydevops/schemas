namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>A core resolver that serves bytes by resolved URI and counts how many bytes of each stream the reader consumed.</summary>
internal sealed class ReadCountingResolver : IResourceResolver
{
    private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);
    private readonly int _maxRead;

    internal ReadCountingResolver(int maxRead = int.MaxValue) => _maxRead = maxRead;

    /// <summary>Gets the streams handed out, in order.</summary>
    internal List<CountingStream> Streams { get; } = [];

    internal ReadCountingResolver Add(string uri, byte[] content)
    {
        _entries[uri] = content;

        return this;
    }

    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_entries.TryGetValue(request.ResolvedUri ?? request.Reference, out var content))
        {
            return ResourceResult.NotFound("no such resource");
        }

        var stream = new CountingStream(content, _maxRead);
        Streams.Add(stream);

        return ResourceResult.Found(stream);
    }

    /// <summary>A read-only stream over bytes that counts the bytes that were read from it.</summary>
    internal sealed class CountingStream : MemoryStream
    {
        private readonly int _maxRead;

        internal CountingStream(byte[] content, int maxRead)
            : base(content, writable: false) => _maxRead = maxRead;

        /// <summary>Gets the number of bytes that were read.</summary>
        internal long BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, Math.Min(count, _maxRead));
            BytesRead += read;

            return read;
        }
    }
}
