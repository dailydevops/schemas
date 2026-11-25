namespace NetEvolve.Schemas.XPath.Xdm.Maps;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A map item (XPath 3.1, 2.1 and 3.11.1; Data Model 3.1, 6.1): an immutable map from atomic keys to sequences, and a function item of
/// arity 1 that returns the value of a key, the empty sequence for a key that the map does not have. It travels as a function item of
/// the data model, so it needs no member of the public contract.
/// </summary>
/// <remarks>
/// Limitation: the signature is <c>function(xs:anyAtomicType) as item()*</c> for every map; a function test that names a narrower result
/// type does not match a map, and the annotations of a map are not part of the model. The order of the entries depends on the hashes of the
/// keys.
/// </remarks>
internal sealed class XdmMap : XdmFunction
{
    private static readonly FunctionSignature MapSignature = new(
        [SequenceTypeInfo.FromParameter(new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ExactlyOne))],
        SequenceTypeInfo.AnyItems
    );

    private readonly HamtMap _entries;

    private XdmMap(HamtMap entries)
        : base(null, MapSignature) => _entries = entries;

    /// <summary>Gets the map without entries.</summary>
    public static XdmMap Empty { get; } = new(HamtMap.Empty);

    /// <summary>Gets the number of entries.</summary>
    public int Count => _entries.Count;

    /// <summary>Reads the map from the value of a function argument.</summary>
    /// <param name="argument">The argument.</param>
    /// <param name="map">The map, <see langword="null"/> unless the argument is exactly one map.</param>
    /// <returns><see langword="true"/> when the argument is exactly one map.</returns>
    public static bool TryRead(in XdmSequence argument, out XdmMap? map)
    {
        map = null;
        if (argument.Count == 1 && argument[0].TryGetFunction(out var function))
        {
            map = function as XdmMap;
        }

        return map is not null;
    }

    /// <summary>Wraps the map as a sequence of one item.</summary>
    /// <returns>The sequence.</returns>
    public XdmSequence ToSequence() => XdmSequence.Create(XdmItem.FromFunction(this));

    /// <summary>Looks up the entry of a key.</summary>
    /// <param name="key">The key.</param>
    /// <param name="entry">The entry.</param>
    /// <returns><see langword="true"/> when the map has an entry with the same key.</returns>
    public bool TryGetEntry(in XdmItem key, out HamtEntry entry) => _entries.TryGetEntry(in key, out entry);

    /// <summary>Looks up the value of a key.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the map has an entry with the same key.</returns>
    public bool TryGet(in XdmItem key, out XdmSequence value) => _entries.TryGet(in key, out value);

    /// <summary>Returns the map with an entry; an entry with the same key is replaced.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The new map; this map is unchanged.</returns>
    public XdmMap Put(in XdmItem key, in XdmSequence value) => new(_entries.Put(in key, value.Materialize()));

    /// <summary>Returns the map without the entry of a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The new map; this map itself when it has no such entry.</returns>
    public XdmMap Remove(in XdmItem key)
    {
        var reduced = _entries.Remove(in key);
        return ReferenceEquals(reduced, _entries) ? this : new(reduced);
    }

    /// <summary>Gets the entries.</summary>
    /// <returns>A new array of the entries, in no specified order.</returns>
    public HamtEntry[] Entries() => _entries.Entries();

    /// <summary>Determines whether every key and value match the types of a map test.</summary>
    /// <param name="key">The type that each key must derive from.</param>
    /// <param name="value">The type that each value must match.</param>
    /// <param name="annotations">The slot that supplies the type annotation of a node.</param>
    /// <returns><see langword="true"/> when every entry matches; also for a map without entries.</returns>
    public bool AllMatch(XdmTypeId key, SequenceTypeMatcher value, IXdmTypeAnnotationSource annotations)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (var entry in _entries.Entries())
        {
            if (!XdmTypes.IsDerivedFrom(entry.Key.TypeId, key) || !value.Matches(entry.Value, annotations))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments)
    {
        ArgumentNullException.ThrowIfNull(context);

        var key = FunctionConversion.Convert(arguments[0], Signature.Parameter(0).Conversion);
        return _entries.TryGet(key[0], out var value) ? value : default;
    }
}
