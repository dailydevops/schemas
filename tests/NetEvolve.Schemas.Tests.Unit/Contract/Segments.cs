namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Buffers;

/// <summary>Builds multi-segment <see cref="ReadOnlySequence{T}"/> values for contract tests.</summary>
internal static class Segments
{
    internal static ReadOnlySequence<T> Create<T>(ReadOnlyMemory<T> first, ReadOnlyMemory<T> second)
    {
        var head = new Segment<T>(first, 0);
        var tail = head.Append(second);
        return new ReadOnlySequence<T>(head, 0, tail, tail.Memory.Length);
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
}
