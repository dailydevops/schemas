namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// A scripted integer datatype. A leading plus sign is lexically valid in 1.1 only; <see cref="int.MinValue"/> stands in for
/// NaN: it is incomparable with every value and never identical to another value.
/// </summary>
internal sealed class FakeDatatypeDescriptor : IDatatypeDescriptor<int>
{
    private readonly IFacet[] _facets;

    public FakeDatatypeDescriptor(
        XsdVersion lexicalMode,
        IDatatypeDescriptor? baseType = null,
        DatatypeVariety variety = DatatypeVariety.Atomic,
        params IFacet[] facets
    )
    {
        LexicalMode = lexicalMode;
        BaseType = baseType;
        Variety = variety;
        _facets = facets;
    }

    public string Name => "fakeInteger";

    public DatatypeVariety Variety { get; }

    public IDatatypeDescriptor? BaseType { get; }

    public XsdVersion LexicalMode { get; }

    public bool IsOrdered => true;

    public IReadOnlyList<IFacet> Facets => _facets;

    public ParseResult<int> Parse(ReadOnlySpan<char> lexical)
    {
        var start = lexical.Length > 0 && lexical[0] == '+' && LexicalMode == XsdVersion.V11 ? 1 : 0;
        if (lexical.Length - start == 0)
        {
            return new ParseResult<int>(DatatypeError.InvalidCharacter, 0);
        }

        if (lexical.Length - start > 9)
        {
            return new ParseResult<int>(DatatypeError.DigitCap, start);
        }

        var value = 0;
        for (var i = start; i < lexical.Length; i++)
        {
            if (lexical[i] is < '0' or > '9')
            {
                return new ParseResult<int>(DatatypeError.InvalidCharacter, i);
            }

            value = (value * 10) + (lexical[i] - '0');
        }

        return new ParseResult<int>(value);
    }

    public ValidationResult Validate(ReadOnlySpan<char> lexical)
    {
        var parsed = Parse(lexical);
        if (!parsed.Success)
        {
            return ValidationResult.Invalid(parsed.Error, FacetKind.None, parsed.ErrorOffset);
        }

        foreach (var facet in _facets)
        {
            if (facet is IValueFacet<int> valueFacet && !valueFacet.IsSatisfiedBy(parsed.Value))
            {
                return ValidationResult.Invalid(DatatypeError.None, facet.Kind, 0);
            }
        }

        return ValidationResult.Valid;
    }

    public XsdOrder Compare(in int a, in int b)
    {
        if (a == int.MinValue || b == int.MinValue)
        {
            return XsdOrder.Incomparable;
        }

        if (a < b)
        {
            return XsdOrder.Less;
        }

        return a > b ? XsdOrder.Greater : XsdOrder.Equal;
    }

    public bool IsIdentical(in int a, in int b) => a != int.MinValue && a == b;
}
