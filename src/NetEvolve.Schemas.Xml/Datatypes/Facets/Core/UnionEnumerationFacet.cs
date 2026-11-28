namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xml.Datatypes.Strings;

/// <summary>
/// The <c>enumeration</c> facet of a union type (Datatypes 1.0, section 4.3.5): an instance satisfies it when it equals one of the listed
/// values in the value space of the member type that accepts it. Each listed value belongs to the first member that accepts it, and the
/// instance to the first member that accepts it, so a value that a different member accepts is not equal even when the lexical forms agree.
/// </summary>
/// <remarks>
/// <para>
/// Equality in a member is <see cref="XsdOrder.Equal"/> from <see cref="IDatatypeDescriptor{TValue}.Compare"/>, or identity when the values are
/// incomparable, for the members of the decimal, integer, float, double, duration, date and time, and boolean families.
/// </para>
/// <para>
/// Limitation: every other member, which includes string-like and binary members and members of variety list or union, compares the lexical
/// forms ordinally after the whitespace of the member is applied (collapse unless the member is a string type that says otherwise); a list or
/// union member is not delegated to its own descriptor, so two list values with the same items in different lexical forms are not equal. A
/// listed value that no member accepts is left out.
/// </para>
/// </remarks>
public sealed class UnionEnumerationFacet : ILexicalFacet
{
    private readonly UnionDescriptor _type;
    private readonly Slot?[] _slots;

    /// <summary>Initializes a new instance of the <see cref="UnionEnumerationFacet"/> class.</summary>
    /// <param name="type">The union type whose members define acceptance and equality.</param>
    /// <param name="values">The listed values in their lexical form.</param>
    public UnionEnumerationFacet(UnionDescriptor type, IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(values);
        _type = type;
        var listed = new List<string>?[type.Members.Count];
        foreach (var value in values)
        {
            if (type.TryGetMemberIndex(value.AsSpan(), out var index))
            {
                (listed[index] ??= []).Add(value);
            }
        }

        _slots = new Slot?[listed.Length];
        for (var i = 0; i < listed.Length; i++)
        {
            if (listed[i] is { } own)
            {
                _slots[i] = Create(type.Members[i], own);
            }
        }
    }

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.Enumeration;

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical) =>
        _type.TryGetMemberIndex(lexical, out var index) && _slots[index] is { } slot && slot.Contains(lexical);

    private static Slot Create(IDatatypeDescriptor member, List<string> values) =>
        member switch
        {
            IDatatypeDescriptor<XsdDecimal> t => Typed(t, values),
            IDatatypeDescriptor<XsdInteger> t => Typed(t, values),
            IDatatypeDescriptor<float> t => Typed(t, values),
            IDatatypeDescriptor<double> t => Typed(t, values),
            IDatatypeDescriptor<XsdDuration> t => Typed(t, values),
            IDatatypeDescriptor<XsdDateTime> t => Typed(t, values),
            IDatatypeDescriptor<bool> t => Typed(t, values),
            _ => new LexicalSlot(ModeOf(member), values),
        };

    private static ValueSlot<TValue> Typed<TValue>(IDatatypeDescriptor<TValue> type, List<string> values)
    {
        var parsed = new List<TValue>(values.Count);
        foreach (var value in values)
        {
            var result = type.Parse(value.AsSpan());
            if (result.Success)
            {
                parsed.Add(result.Value);
            }
        }

        return new ValueSlot<TValue>(type, new EnumerationFacet<TValue>(type, parsed));
    }

    private static WhitespaceMode ModeOf(IDatatypeDescriptor member) =>
        member switch
        {
            AtomicDescriptor<XsdString> t => t.Whitespace,
            AtomicDescriptor<TextRange> t => t.Whitespace,
            _ => WhitespaceMode.Collapse,
        };

    // The listed values of one member, compared in the value space of that member.
    private abstract class Slot
    {
        internal abstract bool Contains(ReadOnlySpan<char> lexical);
    }

    private sealed class ValueSlot<TValue>(IDatatypeDescriptor<TValue> type, EnumerationFacet<TValue> facet) : Slot
    {
        internal override bool Contains(ReadOnlySpan<char> lexical)
        {
            var parsed = type.Parse(lexical);
            return parsed.Success && facet.IsSatisfiedBy(parsed.Value);
        }
    }

    private sealed class LexicalSlot : Slot
    {
        private readonly WhitespaceMode _mode;
        private readonly string[] _values;

        internal LexicalSlot(WhitespaceMode mode, List<string> values)
        {
            _mode = mode;
            _values = new string[values.Count];
            for (var i = 0; i < _values.Length; i++)
            {
                _values[i] = Normalize(values[i].AsSpan(), mode);
            }
        }

        internal override bool Contains(ReadOnlySpan<char> lexical)
        {
            var normalized = WhitespaceNormalizer.Apply(lexical, _mode, out var rented);
            try
            {
                for (var i = 0; i < _values.Length; i++)
                {
                    if (normalized.SequenceEqual(_values[i].AsSpan()))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                rented?.Dispose();
            }
        }

        private static string Normalize(ReadOnlySpan<char> lexical, WhitespaceMode mode)
        {
            var normalized = WhitespaceNormalizer.Apply(lexical, mode, out var rented);
            try
            {
                return normalized.ToString();
            }
            finally
            {
                rented?.Dispose();
            }
        }
    }
}
