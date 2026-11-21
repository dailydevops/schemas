namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using System.Buffers;
using System.Diagnostics;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>Loads a document through one input kind with a time bound and reports the code it ended in.</summary>
internal static class HostileInputs
{
    private const string BaseUri = "http://corpus.test/hostile.xml";
    private const int SegmentSize = 1021;

    /// <summary>The result of one load.</summary>
    /// <param name="Code">The code of the diagnostic, <see langword="null"/> when the load succeeded or did not end.</param>
    /// <param name="Elapsed">The time the load took.</param>
    /// <param name="TimedOut">A value indicating whether the load did not end within the bound.</param>
    internal readonly record struct Outcome(XmlErrorCode? Code, TimeSpan Elapsed, bool TimedOut);

    /// <summary>Builds the options of a corpus file.</summary>
    /// <param name="hostile">The file.</param>
    /// <param name="resolver">The resolver that records the requests of the load.</param>
    /// <returns>The options.</returns>
    internal static XmlReaderOptions OptionsOf(HostileCase hostile, IResourceResolver resolver)
    {
        var options = XmlReaderOptions.Default.WithDtdProcessing(true);
        if (hostile.Limits is { } limits)
        {
            options = options.WithLimits(limits);
        }

        return hostile.Access switch
        {
            HostileAccess.Internal => options,
            HostileAccess.Denied => options.WithExternalAccess(DtdValidationOptions.Default.WithResolver(resolver)),
            _ => options.WithExternalAccess(
                DtdValidationOptions
                    .Default.WithResolver(resolver)
                    .WithAllowExternalSubset(true)
                    .WithAllowExternalGeneralEntities(true)
                    .WithAllowExternalParameterEntities(true)
            ),
        };
    }

    /// <summary>Loads a document through an input kind and reports how it ended.</summary>
    /// <param name="kind">The input kind.</param>
    /// <param name="xml">The document.</param>
    /// <param name="options">The options.</param>
    /// <param name="timeBound">The time the load may take.</param>
    /// <param name="cancellationToken">A token to cancel the load.</param>
    /// <returns>The outcome.</returns>
    internal static async Task<Outcome> LoadAsync(
        HostileInputKind kind,
        string xml,
        XmlReaderOptions options,
        TimeSpan timeBound,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var watch = Stopwatch.StartNew();
        try
        {
            var timedOut = await RunBoundedAsync(
                () => LoadCoreAsync(kind, xml, options, cancellationToken),
                timeBound,
                cancellationToken
            );

            return new Outcome(null, watch.Elapsed, timedOut);
        }
        catch (XmlParseException exception)
        {
            return new Outcome(exception.Diagnostic?.Code, watch.Elapsed, false);
        }
    }

    /// <summary>Runs work and reports whether it did not end within the bound.</summary>
    /// <param name="work">The work.</param>
    /// <param name="timeBound">The time the work may take.</param>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns><see langword="true"/> when the work did not end within the bound.</returns>
    /// <remarks>Work that does not return cannot be aborted; the bound turns it into a failure instead of a hung run.</remarks>
    internal static async Task<bool> RunBoundedAsync(
        Func<Task> work,
        TimeSpan timeBound,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = Task.Run(work, cancellationToken);
        try
        {
            await task.WaitAsync(timeBound, cancellationToken);
        }
        catch (TimeoutException)
        {
            return true;
        }

        return false;
    }

    private static async Task LoadCoreAsync(
        HostileInputKind kind,
        string xml,
        XmlReaderOptions options,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var baseUri = new Uri(BaseUri);
        switch (kind)
        {
            case HostileInputKind.CharSpan:
                _ = XmlDocumentLoader.Load(xml.AsSpan(), options, baseUri);
                break;
            case HostileInputKind.ByteSpan:
                _ = XmlDocumentLoader.Load(Bytes(xml).AsSpan(), options, baseUri);
                break;
            case HostileInputKind.CharMemory:
                _ = XmlDocumentLoader.LoadFromMemory(xml.AsMemory(), options, baseUri);
                break;
            case HostileInputKind.ByteMemory:
                _ = XmlDocumentLoader.LoadFromMemory(Bytes(xml).AsMemory(), options, baseUri);
                break;
            case HostileInputKind.CharSequenceSingle:
                _ = XmlDocumentLoader.Load(Segments(xml.ToCharArray(), int.MaxValue), options, baseUri);
                break;
            case HostileInputKind.ByteSequenceSingle:
                _ = XmlDocumentLoader.Load(Segments(Bytes(xml), int.MaxValue), options, baseUri);
                break;
            case HostileInputKind.CharSequenceSegmented:
                _ = XmlDocumentLoader.Load(Segments(xml.ToCharArray(), SegmentOf(xml.Length)), options, baseUri);
                break;
            case HostileInputKind.ByteSequenceSegmented:
                _ = XmlDocumentLoader.Load(Segments(Bytes(xml), SegmentOf(Bytes(xml).Length)), options, baseUri);
                break;
            default:
                using (var stream = new ShortReadStream(Bytes(xml), SegmentOf(Bytes(xml).Length)))
                {
                    _ = await XmlDocumentLoader.LoadAsync(stream, options, baseUri, cancellationToken);
                }

                break;
        }
    }

    // Large documents are cut at a fixed size; a small one is cut in halves at most, so no document arrives in one piece.
    private static int SegmentOf(int length) => Math.Min(SegmentSize, Math.Max(1, length / 2));

    private static byte[] Bytes(string xml) => new UTF8Encoding(false).GetBytes(xml);

    private static ReadOnlySequence<T> Segments<T>(T[] data, int size)
    {
        if (data.Length == 0)
        {
            return ReadOnlySequence<T>.Empty;
        }

        Segment<T>? head = null;
        Segment<T>? tail = null;
        for (var offset = 0; offset < data.Length; offset += size)
        {
            var part = data.AsMemory(offset, Math.Min(size, data.Length - offset));
            tail = tail is null ? (head = new Segment<T>(part, 0)) : tail.Append(part);
        }

        return new ReadOnlySequence<T>(head!, 0, tail!, tail!.Memory.Length);
    }

    private sealed class Segment<T> : ReadOnlySequenceSegment<T>
    {
        internal Segment(ReadOnlyMemory<T> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        internal Segment<T> Append(ReadOnlyMemory<T> memory)
        {
            var next = new Segment<T>(memory, RunningIndex + Memory.Length);
            Next = next;

            return next;
        }
    }

    // A readable stream that returns at most a fixed number of bytes per read, so no document arrives in one piece.
    private sealed class ShortReadStream(byte[] data, int maxRead) : Stream
    {
        private int _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var take = Math.Min(Math.Min(count, maxRead), data.Length - _position);
            Array.Copy(data, _position, buffer, offset, take);
            _position += take;

            return take;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var take = Math.Min(Math.Min(buffer.Length, maxRead), data.Length - _position);
            data.AsMemory(_position, take).CopyTo(buffer);
            _position += take;

            return new ValueTask<int>(take);
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
