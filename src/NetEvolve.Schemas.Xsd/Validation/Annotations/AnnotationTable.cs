namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;

/// <summary>The sink that keeps the annotations of one run for reading by ordinal.</summary>
internal sealed class AnnotationTable : IAnnotationSink
{
    /// <summary>Gets the highest ordinal that an element record was stored for, zero for none.</summary>
    public int ElementOrdinalLimit => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the number of attribute records.</summary>
    public int AttributeCount => throw new NotSupportedException("Not implemented yet.");

    /// <inheritdoc />
    public void Element(in AnnotationRecord record) => throw new NotSupportedException("Not implemented yet.");

    /// <inheritdoc />
    public void Attribute(in AnnotationRecord record) => throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the record of an element.</summary>
    /// <param name="ordinal">The ordinal of the element start event.</param>
    /// <param name="record">The record.</param>
    /// <returns><see langword="true"/> when the element was annotated.</returns>
    public bool TryGetElement(int ordinal, out AnnotationRecord record) =>
        throw new NotSupportedException("Not implemented yet.");

    /// <summary>Gets the record of an attribute.</summary>
    /// <param name="ordinal">The ordinal of the owner element.</param>
    /// <param name="index">The index of the attribute among the attributes of the owner.</param>
    /// <param name="record">The record.</param>
    /// <returns><see langword="true"/> when the attribute was annotated.</returns>
    public bool TryGetAttribute(int ordinal, int index, out AnnotationRecord record) =>
        throw new NotSupportedException("Not implemented yet.");
}
