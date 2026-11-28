namespace NetEvolve.Schemas.Xsd.Validation.Tree;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

/// <summary>
/// The annotation sink of a tree run that also keeps the defaults of the schema, which the copy takes as nodes (XSD-79, #361; design #146
/// DP-19, option A): it hands every record on to the sink that stores the annotations, and it remembers the defaulted attributes of the
/// element whose start tag ends and the default or fixed value of the element that ends, until the tree sink has put them into the copy.
/// </summary>
/// <remarks>Not thread-safe; one instance serves one run.</remarks>
internal sealed class TreeDefaults : IAnnotationSink
{
    private readonly IAnnotationSink _next;
    private readonly List<AnnotationRecord> _attributes = [];
    private string? _value;

    /// <summary>Initializes a new instance of the <see cref="TreeDefaults"/> class.</summary>
    /// <param name="next">The sink that stores the annotations.</param>
    internal TreeDefaults(IAnnotationSink next)
    {
        ArgumentNullException.ThrowIfNull(next);

        _next = next;
    }

    /// <inheritdoc />
    public void Element(in AnnotationRecord record)
    {
        _next.Element(in record);

        // The record of an element comes with the end tag of that element, which the tree sink takes the value for right after.
        if (record.DefaultedValue is not null)
        {
            _value = record.DefaultedValue;
        }
    }

    /// <inheritdoc />
    public void Attribute(in AnnotationRecord record)
    {
        _next.Attribute(in record);
        if (record.DefaultedValue is not null)
        {
            _attributes.Add(record);
        }
    }

    /// <summary>Adds the defaulted attributes that the end of a start tag reported to the copy, after the specified ones, and forgets them.</summary>
    /// <param name="copy">The builder of the copy, with the start tag of the element open.</param>
    internal void CopyAttributes(XmlTreeBuilder copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        for (var i = 0; i < _attributes.Count; i++)
        {
            var record = _attributes[i];
            var value = record.DefaultedValue!;
            copy.Attribute(record.DefaultedName, value.AsSpan());
            copy.AttributeInfo(new ValidationAttributeInfo(default, value.AsMemory(), null, false, default));
        }

        _attributes.Clear();
    }

    /// <summary>Takes the default or fixed value that the element which just ended took, <see langword="null"/> when it took none.</summary>
    /// <returns>The value.</returns>
    internal string? TakeValue()
    {
        var value = _value;
        _value = null;

        return value;
    }
}
