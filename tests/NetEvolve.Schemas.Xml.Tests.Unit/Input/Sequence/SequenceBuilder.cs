namespace NetEvolve.Schemas.Xml.Tests.Unit.Input.Sequence;

using System;
using System.Buffers;
using System.Collections.Generic;

/// <summary>Builds multi-segment <see cref="ReadOnlySequence{T}"/> values from one array: cut at offsets, in even pieces, with empty segments.</summary>
internal static class SequenceBuilder
{
    /// <summary>One segment holding everything.</summary>
    internal static ReadOnlySequence<T> Whole<T>(T[] data) => FromParts([data]);

    /// <summary>Cuts the data at the given offsets, which must not decrease; equal offsets give empty segments.</summary>
    internal static ReadOnlySequence<T> Split<T>(T[] data, params int[] offsets)
    {
        var parts = new List<T[]>();
        var start = 0;
        foreach (var offset in offsets)
        {
            parts.Add(data.AsSpan(start, offset - start).ToArray());
            start = offset;
        }

        parts.Add(data.AsSpan(start).ToArray());
        return FromParts(parts);
    }

    /// <summary>Cuts the data into segments of <paramref name="size"/> elements, optionally with an empty segment around every one.</summary>
    internal static ReadOnlySequence<T> Chunks<T>(T[] data, int size, bool withEmptySegments = false)
    {
        var parts = new List<T[]>();
        if (withEmptySegments)
        {
            parts.Add([]);
        }

        for (var i = 0; i < data.Length; i += size)
        {
            parts.Add(data.AsSpan(i, Math.Min(size, data.Length - i)).ToArray());
            if (withEmptySegments)
            {
                parts.Add([]);
            }
        }

        return FromParts(parts);
    }

    /// <summary>Cuts the data into segments whose sizes come from a seeded generator, so a failure repeats.</summary>
    internal static ReadOnlySequence<T> Random<T>(T[] data, int seed, int max)
    {
        var random = new System.Random(seed);
        var parts = new List<T[]>();
        for (var i = 0; i < data.Length;)
        {
            var take = Math.Min(random.Next(0, max + 1), data.Length - i);
            parts.Add(data.AsSpan(i, take).ToArray());
            i += take;
        }

        return FromParts(parts);
    }

    internal static ReadOnlySequence<T> FromParts<T>(IReadOnlyList<T[]> parts)
    {
        if (parts.Count == 0)
        {
            return ReadOnlySequence<T>.Empty;
        }

        var head = new Segment<T>(parts[0], 0);
        var tail = head;
        for (var i = 1; i < parts.Count; i++)
        {
            tail = tail.Append(parts[i]);
        }

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
