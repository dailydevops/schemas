namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using NetEvolve.Arguments;

/// <summary>
/// The sink that keeps the annotations of one run for reading by ordinal (design #146 DP-18, decision 2): the element records in an
/// array indexed by the ordinal of the element start event, so a lookup is constant time and an element that was never annotated is a gap,
/// and the attribute records in the order they arrive, which is ascending by owner ordinal and attribute index, so a lookup is a binary search.
/// </summary>
/// <remarks>
/// The arrays are allocated on the first record, so a table that no run fed costs one object. One table serves one run; it is not
/// thread-safe while the run writes and is safe for concurrent reads afterwards. Limitation: memory grows with the number of elements up to
/// the highest annotated ordinal and with the number of attributes, bounded only by the size of the input; a consumer that does not need
/// annotations registers no sink.
/// </remarks>
internal sealed class AnnotationTable : IAnnotationSink
{
    private const int MinimumCapacity = 16;

    private AnnotationRecord[] _elements = [];
    private AnnotationRecord[] _attributes = [];
    private int _attributeCount;
    private int _limit;

    /// <summary>Gets the highest ordinal that an element record was stored for, zero for none.</summary>
    public int ElementOrdinalLimit => _limit;

    /// <summary>Gets the number of attribute records.</summary>
    public int AttributeCount => _attributeCount;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">The ordinal of the record is less than one.</exception>
    public void Element(in AnnotationRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Ordinal, 1);

        if (record.Ordinal >= _elements.Length)
        {
            Array.Resize(ref _elements, Math.Max(MinimumCapacity, Math.Max(_elements.Length * 2, record.Ordinal + 1)));
        }

        _elements[record.Ordinal] = record;
        if (record.Ordinal > _limit)
        {
            _limit = record.Ordinal;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">The ordinal of the record is less than one or its attribute index is negative.</exception>
    public void Attribute(in AnnotationRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(record.Ordinal, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(record.AttributeIndex, 0);

        if (_attributeCount == _attributes.Length)
        {
            Array.Resize(ref _attributes, Math.Max(MinimumCapacity, _attributeCount * 2));
        }

        _attributes[_attributeCount++] = record;
    }

    /// <summary>Gets the record of an element.</summary>
    /// <param name="ordinal">The ordinal of the element start event.</param>
    /// <param name="record">The record.</param>
    /// <returns><see langword="true"/> when the element was annotated.</returns>
    public bool TryGetElement(int ordinal, out AnnotationRecord record)
    {
        if (ordinal < 1 || ordinal >= _elements.Length)
        {
            record = default;
            return false;
        }

        // A gap holds the default record, whose ordinal is zero.
        record = _elements[ordinal];
        return record.Ordinal == ordinal;
    }

    /// <summary>Gets the record of an attribute.</summary>
    /// <param name="ordinal">The ordinal of the owner element.</param>
    /// <param name="index">The index of the attribute among the attributes of the owner.</param>
    /// <param name="record">The record.</param>
    /// <returns><see langword="true"/> when the attribute was annotated.</returns>
    public bool TryGetAttribute(int ordinal, int index, out AnnotationRecord record)
    {
        var low = 0;
        var high = _attributeCount - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var candidate = _attributes[middle];
            var order =
                candidate.Ordinal != ordinal
                    ? candidate.Ordinal.CompareTo(ordinal)
                    : candidate.AttributeIndex.CompareTo(index);
            if (order == 0)
            {
                record = candidate;
                return true;
            }

            if (order < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        record = default;
        return false;
    }
}
