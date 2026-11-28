namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// An atomic datatype: a primitive built from an <see cref="ILexicalMapping{TValue}"/>, or a restriction of another atomic
/// type that adds facets and may replace the whitespace mode. The pipeline is whitespace, lexical check, value mapping, facets.
/// </summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
/// <remarks>Limitation: a restriction may set any whitespace mode; the rule that it can only strengthen the base mode is not checked.</remarks>
public sealed class AtomicDescriptor<TValue> : IDatatypeDescriptor<TValue>
{
    private const int ScratchLength = 128;

    private readonly ILexicalMapping<TValue> _mapping;
    private readonly IFusedLexicalMapping<TValue>? _fused;
    private readonly bool _hasValueFacets;
    private readonly ILexicalFacet[] _lexicalFacets;
    private readonly IValueFacet<TValue>[] _valueFacets;

    /// <summary>Initializes a primitive type.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="lexicalMode">The XSD version whose rules apply.</param>
    /// <param name="mapping">The lexical mapping.</param>
    public AtomicDescriptor(string name, XsdVersion lexicalMode, ILexicalMapping<TValue> mapping)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(mapping);
        Name = name;
        LexicalMode = lexicalMode;
        _mapping = mapping;
        _fused = mapping as IFusedLexicalMapping<TValue>;
        Whitespace = mapping.Whitespace;
        Facets = [];
        _lexicalFacets = [];
        _valueFacets = [];
    }

    /// <summary>Initializes a restriction of an atomic type.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="baseType">The restricted type; its mapping, version, and facets are inherited.</param>
    /// <param name="whitespace">The whitespace mode, <see langword="null"/> to inherit it.</param>
    /// <param name="facets">The facets added by the restriction, after the inherited ones.</param>
    public AtomicDescriptor(
        string name,
        AtomicDescriptor<TValue> baseType,
        WhitespaceMode? whitespace,
        IReadOnlyList<IFacet> facets
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(facets);
        Name = name;
        BaseType = baseType;
        LexicalMode = baseType.LexicalMode;
        _mapping = baseType._mapping;
        _fused = baseType._fused;
        Whitespace = whitespace ?? baseType.Whitespace;
        Facets = Combine(baseType.Facets, facets, out _hasValueFacets);
        _lexicalFacets = Select<ILexicalFacet>(Facets);
        _valueFacets = FoldBounds(Select<IValueFacet<TValue>>(Facets));
    }

    /// <summary>Initializes a restriction of an atomic type whose value type differs from the value type of this descriptor, for example integer on <see cref="System.Numerics.BigInteger"/> derived from decimal.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="baseType">The restricted type; its version and facets are inherited, its mapping is not.</param>
    /// <param name="mapping">The lexical mapping of this type.</param>
    /// <param name="whitespace">The whitespace mode, <see langword="null"/> to take the mode of <paramref name="mapping"/>.</param>
    /// <param name="facets">The facets added by the restriction, after the inherited ones.</param>
    /// <remarks>Limitation: the inherited facets are carried over as listed; value facets of another value type are never applied.</remarks>
    public AtomicDescriptor(
        string name,
        IDatatypeDescriptor baseType,
        ILexicalMapping<TValue> mapping,
        WhitespaceMode? whitespace,
        IReadOnlyList<IFacet> facets
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(facets);
        Name = name;
        BaseType = baseType;
        LexicalMode = baseType.LexicalMode;
        _mapping = mapping;
        _fused = mapping as IFusedLexicalMapping<TValue>;
        Whitespace = whitespace ?? mapping.Whitespace;
        Facets = Combine(baseType.Facets, facets, out _hasValueFacets);
        _lexicalFacets = Select<ILexicalFacet>(Facets);
        _valueFacets = FoldBounds(Select<IValueFacet<TValue>>(Facets));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public DatatypeVariety Variety => DatatypeVariety.Atomic;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType { get; }

    /// <inheritdoc />
    public XsdVersion LexicalMode { get; }

    /// <inheritdoc />
    public bool IsOrdered => _mapping.IsOrdered;

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets { get; }

    /// <summary>Gets the whitespace mode applied before the lexical check.</summary>
    public WhitespaceMode Whitespace { get; }

    /// <inheritdoc />
    public ParseResult<TValue> Parse(ReadOnlySpan<char> lexical)
    {
        var error = Evaluate(lexical, true, out var value, out _, out var offset);
        return error == DatatypeError.None ? new ParseResult<TValue>(value) : new ParseResult<TValue>(error, offset);
    }

    /// <inheritdoc />
    public ValidationResult Validate(ReadOnlySpan<char> lexical)
    {
        var error = Evaluate(lexical, false, out _, out var facet, out var offset);
        return error == DatatypeError.None
            ? ValidationResult.Valid
            : ValidationResult.Invalid(
                error == DatatypeError.FacetViolation ? DatatypeError.None : error,
                facet,
                offset
            );
    }

    /// <inheritdoc />
    public XsdOrder Compare(in TValue a, in TValue b) => _mapping.Compare(in a, in b, LexicalMode);

    /// <inheritdoc />
    public bool IsIdentical(in TValue a, in TValue b) => _mapping.IsIdentical(in a, in b, LexicalMode);

    // The facets of one kind in their order, so the validation loops need no type test per facet.
    private static T[] Select<T>(IReadOnlyList<IFacet> facets)
        where T : class
    {
        var selected = new List<T>(facets.Count);
        for (var i = 0; i < facets.Count; i++)
        {
            if (facets[i] is T facet)
            {
                selected.Add(facet);
            }
        }

        return [.. selected];
    }

    // Evaluation only: a bound that a stricter bound of the same kind implies cannot change the outcome, because both report the same facet kind.
    // Of equal bounds the first stays. The public facet list is not folded.
    internal static IValueFacet<TValue>[] FoldBounds(IValueFacet<TValue>[] facets)
    {
        var kept = new List<IValueFacet<TValue>>(facets.Length);
        for (var i = 0; i < facets.Length; i++)
        {
            if (!IsRedundant(facets, i))
            {
                kept.Add(facets[i]);
            }
        }

        return kept.Count == facets.Length ? facets : [.. kept];
    }

    private static bool IsRedundant(IValueFacet<TValue>[] facets, int index)
    {
        if (facets[index] is not IFoldableBound<TValue> bound)
        {
            return false;
        }

        for (var j = 0; j < facets.Length; j++)
        {
            if (
                j != index
                && bound.IsImpliedBy(facets[j])
                && (j < index || !(facets[j] is IFoldableBound<TValue> other && other.IsImpliedBy(facets[index])))
            )
            {
                return true;
            }
        }

        return false;
    }

    private static IFacet[] Combine(
        IReadOnlyList<IFacet> inherited,
        IReadOnlyList<IFacet> added,
        out bool hasValueFacets
    )
    {
        var all = new IFacet[inherited.Count + added.Count];
        for (var i = 0; i < inherited.Count; i++)
        {
            all[i] = inherited[i];
        }

        for (var i = 0; i < added.Count; i++)
        {
            all[inherited.Count + i] = added[i];
        }

        hasValueFacets = false;
        for (var i = 0; i < all.Length; i++)
        {
            hasValueFacets |= all[i] is IValueFacet<TValue>;
        }

        return all;
    }

    private DatatypeError Evaluate(
        ReadOnlySpan<char> lexical,
        bool wantValue,
        out TValue value,
        out FacetKind failedFacet,
        out int offset
    ) =>
        // A lexical form that is already normalized, the common case, needs neither a copy nor a scratch buffer.
        WhitespaceNormalizer.IsNormalized(lexical, Whitespace)
            ? EvaluateNormalized(lexical, lexical, wantValue, out value, out failedFacet, out offset)
            : EvaluateChanged(lexical, wantValue, out value, out failedFacet, out offset);

    private DatatypeError EvaluateChanged(
        ReadOnlySpan<char> lexical,
        bool wantValue,
        out TValue value,
        out FacetKind failedFacet,
        out int offset
    )
    {
        // A lexical form that changes and is short is normalized on the stack, so the common case allocates nothing.
        Span<char> scratch = stackalloc char[ScratchLength];
        var normalized = WhitespaceNormalizer.Apply(lexical, Whitespace, scratch, out var rented);
        try
        {
            return EvaluateNormalized(lexical, normalized, wantValue, out value, out failedFacet, out offset);
        }
        finally
        {
            rented?.Dispose();
        }
    }

    private DatatypeError EvaluateNormalized(
        ReadOnlySpan<char> lexical,
        ReadOnlySpan<char> normalized,
        bool wantValue,
        out TValue value,
        out FacetKind failedFacet,
        out int offset
    )
    {
        value = default!;
        failedFacet = FacetKind.None;
        offset = 0;

        // A fused mapping scans the form once for the check and the value; without a need for the value, Check is cheaper.
        var needValue = wantValue || _hasValueFacets;
        var fused = needValue ? _fused : null;
        var error = fused is null
            ? _mapping.Check(normalized, LexicalMode, out var normalizedOffset)
            : fused.TryMap(normalized, Whitespace, LexicalMode, out value, out normalizedOffset);
        if (error != DatatypeError.None)
        {
            offset = WhitespaceNormalizer.ToSourceOffset(lexical, Whitespace, normalizedOffset);
            return error;
        }

        // Facets in the fixed order of the design: lexical facets first, then the value-space facets.
        foreach (var lexicalFacet in _lexicalFacets)
        {
            if (!lexicalFacet.IsSatisfiedBy(normalized))
            {
                failedFacet = lexicalFacet.Kind;
                return DatatypeError.FacetViolation;
            }
        }

        if (needValue)
        {
            if (fused is null)
            {
                value = _mapping.Map(normalized, Whitespace, LexicalMode);
            }

            foreach (var valueFacet in _valueFacets)
            {
                if (!valueFacet.IsSatisfiedBy(in value))
                {
                    failedFacet = valueFacet.Kind;
                    return DatatypeError.FacetViolation;
                }
            }
        }

        return DatatypeError.None;
    }
}
