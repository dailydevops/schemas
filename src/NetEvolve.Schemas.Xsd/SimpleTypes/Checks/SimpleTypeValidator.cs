namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xml.Datatypes.Strings;

/// <summary>
/// The compiled validator of a <see cref="Definitions.SimpleTypeDefinition"/> that adds facets of its own to the built-in type it derives from
/// (Datatypes section 4.3; design #146 DP-9). An atomic type is validated by a descriptor of the Xml package that restricts the
/// built-in descriptor with the facets, so it allocates nothing per value; a list or union checks its own lexical facets over the
/// whole lexical form, after its items or members were assessed.
/// </summary>
/// <remarks>
/// Limitations: only a built-in descriptor that is an <see cref="AtomicDescriptor{TValue}"/> can be restricted, so the facets on
/// <c>anyURI</c> and on a datatype this package does not know are not evaluated; a list or union checks lexical facets only, and
/// a list collapses whitespace before it does; the facets that <c>FacetFactory</c> does not carry (<c>enumeration</c> on a base
/// that is not string-like, <c>explicitTimezone</c>, assertions) are not evaluated.
/// </remarks>
internal sealed class SimpleTypeValidator
{
    private const int Scratch = 256;

    private readonly IDatatypeDescriptor? _atomic;
    private readonly ILexicalFacet[] _lexical;
    private readonly bool _collapse;

    private SimpleTypeValidator(IDatatypeDescriptor? atomic, ILexicalFacet[] lexical, bool collapse)
    {
        _atomic = atomic;
        _lexical = lexical;
        _collapse = collapse;
    }

    /// <summary>Compiles the facets that a definition adds to its built-in root.</summary>
    /// <param name="name">The name for the descriptor.</param>
    /// <param name="variety">The variety.</param>
    /// <param name="datatype">The built-in datatype at the root of the derivation, <see langword="null"/> for a user-defined list or union.</param>
    /// <param name="facets">All facets of the definition: the built-in ones, then the added ones.</param>
    /// <param name="builtInCount">The number of leading facets that belong to <paramref name="datatype"/>.</param>
    /// <returns>The validator, <see langword="null"/> when there is nothing to evaluate beyond the base type.</returns>
    internal static SimpleTypeValidator? Create(
        string name,
        DatatypeVariety variety,
        IDatatypeDescriptor? datatype,
        IReadOnlyList<IFacet> facets,
        int builtInCount
    )
    {
        if (facets.Count <= builtInCount)
        {
            return null;
        }

        // The length facets are no-ops for QName and NOTATION (Datatypes 1.0 section 4.3.1), so they are not evaluated.
        var lengthless = datatype?.Name is "QName" or "NOTATION";
        var own = new List<IFacet>(facets.Count - builtInCount);
        for (var i = builtInCount; i < facets.Count; i++)
        {
            if (!lengthless || facets[i].Kind is not (FacetKind.Length or FacetKind.MinLength or FacetKind.MaxLength))
            {
                own.Add(facets[i]);
            }
        }

        if (own.Count == 0)
        {
            return null;
        }

        if (variety == DatatypeVariety.Atomic)
        {
            WhitespaceMode? whitespace = null;
            foreach (var facet in own)
            {
                if (facet is WhiteSpaceFacet space)
                {
                    whitespace = space.Mode;
                }
            }

            var atomic = Derive(name, datatype, whitespace, own);
            return atomic is null ? null : new SimpleTypeValidator(atomic, [], false);
        }

        var lexical = new List<ILexicalFacet>(own.Count);
        foreach (var facet in own)
        {
            if (facet is ILexicalFacet lexicalFacet)
            {
                lexical.Add(lexicalFacet);
            }
        }

        return lexical.Count == 0 ? null : new SimpleTypeValidator(null, [.. lexical], variety == DatatypeVariety.List);
    }

    /// <summary>Validates a lexical form: an atomic type completely, a list or union against its own facets only.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The result; the failed facet when a facet is not satisfied.</returns>
    internal ValidationResult Validate(ReadOnlySpan<char> lexical)
    {
        if (_atomic is not null)
        {
            return _atomic.Validate(lexical);
        }

        if (!_collapse || IsCollapsed(lexical))
        {
            return Check(lexical);
        }

        var rented = lexical.Length > Scratch ? ArrayPool<char>.Shared.Rent(lexical.Length) : null;
        try
        {
            var buffer = rented is null ? stackalloc char[Scratch] : new Span<char>(rented);
            return Check(buffer.Slice(0, Collapse(lexical, buffer)));
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static IDatatypeDescriptor? Derive(
        string name,
        IDatatypeDescriptor? datatype,
        WhitespaceMode? whitespace,
        IReadOnlyList<IFacet> own
    ) =>
        datatype switch
        {
            AtomicDescriptor<XsdString> d => new AtomicDescriptor<XsdString>(name, d, whitespace, own),
            AtomicDescriptor<XsdDecimal> d => new AtomicDescriptor<XsdDecimal>(name, d, whitespace, own),
            AtomicDescriptor<XsdInteger> d => new AtomicDescriptor<XsdInteger>(name, d, whitespace, own),
            AtomicDescriptor<XsdDateTime> d => new AtomicDescriptor<XsdDateTime>(name, d, whitespace, own),
            AtomicDescriptor<XsdDuration> d => new AtomicDescriptor<XsdDuration>(name, d, whitespace, own),
            AtomicDescriptor<XsdBinary> d => new AtomicDescriptor<XsdBinary>(name, d, whitespace, own),
            AtomicDescriptor<TextRange> d => new AtomicDescriptor<TextRange>(name, d, whitespace, own),
            AtomicDescriptor<double> d => new AtomicDescriptor<double>(name, d, whitespace, own),
            AtomicDescriptor<float> d => new AtomicDescriptor<float>(name, d, whitespace, own),
            AtomicDescriptor<bool> d => new AtomicDescriptor<bool>(name, d, whitespace, own),
            _ => null,
        };

    private ValidationResult Check(ReadOnlySpan<char> lexical)
    {
        for (var i = 0; i < _lexical.Length; i++)
        {
            if (!_lexical[i].IsSatisfiedBy(lexical))
            {
                return ValidationResult.Invalid(DatatypeError.None, _lexical[i].Kind, 0);
            }
        }

        return ValidationResult.Valid;
    }

    private static bool IsCollapsed(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '\t' or '\n' or '\r' || (c == ' ' && (i == 0 || i == text.Length - 1 || text[i - 1] == ' ')))
            {
                return false;
            }
        }

        return true;
    }

    private static int Collapse(ReadOnlySpan<char> text, Span<char> destination)
    {
        var length = 0;
        var pending = false;
        foreach (var c in text)
        {
            if (c is ' ' or '\t' or '\n' or '\r')
            {
                pending = length > 0;
                continue;
            }

            if (pending)
            {
                destination[length++] = ' ';
                pending = false;
            }

            destination[length++] = c;
        }

        return length;
    }
}
