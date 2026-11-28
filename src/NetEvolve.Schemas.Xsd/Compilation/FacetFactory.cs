namespace NetEvolve.Schemas.Xsd.Compilation;

using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>
/// Turns the facets of a restriction step, which the facet check has validated, into the facet objects of the datatype contract
/// that the new simple type definition carries.
/// </summary>
/// <remarks>
/// Limitations: the <c>enumeration</c> facet is carried for string-like bases (including anyURI) and for the ordered families (decimal, integer, float,
/// double, duration, date and time, boolean), not for the others (binary, <c>QName</c>, and <c>NOTATION</c>); the
/// <c>assertion</c> facets (XSD 1.1, XSD-78) are not carried here, they are collected separately; a length of a binary type is carried
/// by the binary length facet only when the base is a built-in type of that family. A facet that does not parse is left out, which
/// can only follow an issue that the check has reported.
/// </remarks>
internal static class FacetFactory
{
    /// <summary>Creates the facet objects of one restriction step.</summary>
    /// <param name="baseType">The resolved base type.</param>
    /// <param name="step">The facets of the step in document order.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns>The facets in document order, the patterns of the step combined into one facet at the first pattern.</returns>
    internal static IReadOnlyList<IFacet> Create(
        IXsdComponent baseType,
        IReadOnlyList<FacetDeclaration> step,
        XsdVersion version
    )
    {
        ArgumentNullException.ThrowIfNull(baseType);

        return Create(step, FacetBase.Resolve(baseType, version), version, null);
    }

    /// <summary>Creates the facet objects of one restriction step from a base description that the caller has already resolved.</summary>
    /// <param name="step">The facets of the step in document order.</param>
    /// <param name="facts">The description of the base type, as <see cref="FacetBase.Resolve"/> returned it.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="compiledPattern">The facet that the check compiled from the only pattern of the step, or <see langword="null"/>.</param>
    /// <returns>The facets in document order, the patterns of the step combined into one facet at the first pattern.</returns>
    internal static IReadOnlyList<IFacet> Create(
        IReadOnlyList<FacetDeclaration> step,
        FacetBase? facts,
        XsdVersion version,
        PatternFacet? compiledPattern
    )
    {
        ArgumentNullException.ThrowIfNull(step);

        var facets = new List<IFacet>(step.Count);
        var patterns = new List<string>();
        var enumeration = new List<string>();
        var patternSlot = -1;
        var enumerationSlot = -1;
        for (var i = 0; i < step.Count; i++)
        {
            var declaration = step[i];
            switch (declaration.Kind)
            {
                case FacetKind.Pattern:
                    patternSlot = Slot(patternSlot, patterns.Count, facets);
                    patterns.Add(declaration.Value);
                    break;
                case FacetKind.Enumeration:
                    enumerationSlot = Slot(enumerationSlot, enumeration.Count, facets);
                    enumeration.Add(declaration.Value);
                    break;
                default:
                    AddFacet(facets, declaration, facts);
                    break;
            }
        }

        if (patterns.Count == 1 && compiledPattern is not null)
        {
            facets[patternSlot] = compiledPattern;
        }
        else if (patterns.Count > 0 && PatternFacet.TryCreate(patterns, version, null, out var pattern, out _))
        {
            facets[patternSlot] = pattern!;
        }

        if (enumeration.Count > 0 && facts is { Variety: DatatypeVariety.Atomic })
        {
            facets[enumerationSlot] = facts.Primitive is PrimitiveKind.StringType or PrimitiveKind.AnyUri
                ? new LexicalEnumerationFacet(enumeration)
                : Enumeration(enumeration, facts.Descriptor)!;
        }

        if (enumeration.Count > 0 && facts is { Variety: DatatypeVariety.List, Item: { } item })
        {
            facets[enumerationSlot] = ListEnumeration(enumeration, item)!;
        }

        _ = facets.RemoveAll(static facet => facet is null);
        return facets;
    }

    // A slot holds the place of a facet that is built from every declaration of its kind once the step is read.
    private static int Slot(int slot, int count, List<IFacet> facets)
    {
        if (count > 0)
        {
            return slot;
        }

        facets.Add(null!);
        return facets.Count - 1;
    }

    private static void AddFacet(List<IFacet> facets, FacetDeclaration declaration, FacetBase? facts)
    {
        var facet = declaration.Kind switch
        {
            FacetKind.WhiteSpace => WhiteSpace(declaration.Value),
            FacetKind.Length or FacetKind.MinLength or FacetKind.MaxLength => Length(declaration, facts),
            FacetKind.TotalDigits or FacetKind.FractionDigits => Digits(declaration, facts),
            FacetKind.MaxInclusive or FacetKind.MaxExclusive or FacetKind.MinExclusive or FacetKind.MinInclusive =>
                Bound(declaration, facts),
            FacetKind.ExplicitTimezone => ExplicitTimezone(declaration.Value),
            _ => (IFacet?)null,
        };
        if (facet is not null)
        {
            facets.Add(facet);
        }
    }

    // Every listed value is a whitespace-separated item sequence; a sequence with an item that does not parse is left out, the check has reported it.
    private static IFacet? ListEnumeration(List<string> values, FacetBase item) =>
        item.Primitive is PrimitiveKind.StringType or PrimitiveKind.AnyUri
            ? new ListLexicalEnumerationFacet(values)
            : item.Descriptor switch
            {
                IDatatypeDescriptor<XsdDecimal> t => ListEnumeration(values, t),
                IDatatypeDescriptor<XsdInteger> t => ListEnumeration(values, t),
                IDatatypeDescriptor<float> t => ListEnumeration(values, t),
                IDatatypeDescriptor<double> t => ListEnumeration(values, t),
                IDatatypeDescriptor<XsdDuration> t => ListEnumeration(values, t),
                IDatatypeDescriptor<XsdDateTime> t => ListEnumeration(values, t),
                IDatatypeDescriptor<bool> t => ListEnumeration(values, t),
                _ => null,
            };

    private static ListEnumerationFacet<TValue>? ListEnumeration<TValue>(
        List<string> values,
        IDatatypeDescriptor<TValue> type
    )
    {
        var sequences = new List<TValue[]>(values.Count);
        foreach (var value in values)
        {
            var items = new List<TValue>();
            var cursor = new XsdListCursor(new TextRange(0, value.Length));
            var valid = true;
            while (valid && cursor.TryNext(value.AsSpan(), out var range))
            {
                var result = type.Parse(range.Slice(value.AsSpan()));
                valid = result.Success;
                if (valid)
                {
                    items.Add(result.Value);
                }
            }

            if (valid)
            {
                sequences.Add([.. items]);
            }
        }

        return sequences.Count == 0 ? null : new ListEnumerationFacet<TValue>(type, sequences);
    }

    private static IFacet? Enumeration(List<string> values, IDatatypeDescriptor? type) =>
        type switch
        {
            IDatatypeDescriptor<XsdDecimal> t => Enumeration(values, t),
            IDatatypeDescriptor<XsdInteger> t => Enumeration(values, t),
            IDatatypeDescriptor<float> t => Enumeration(values, t),
            IDatatypeDescriptor<double> t => Enumeration(values, t),
            IDatatypeDescriptor<XsdDuration> t => Enumeration(values, t),
            IDatatypeDescriptor<XsdDateTime> t => Enumeration(values, t),
            IDatatypeDescriptor<bool> t => Enumeration(values, t),
            _ => null,
        };

    // A value that does not parse is left out; the facet check has reported it.
    private static EnumerationFacet<TValue>? Enumeration<TValue>(List<string> values, IDatatypeDescriptor<TValue> type)
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

        return parsed.Count == 0 ? null : new EnumerationFacet<TValue>(type, parsed);
    }

    private static ExplicitTimezoneFacet? ExplicitTimezone(string value) =>
        value switch
        {
            "required" => new ExplicitTimezoneFacet(ExplicitTimezoneMode.Required),
            "prohibited" => new ExplicitTimezoneFacet(ExplicitTimezoneMode.Prohibited),
            "optional" => new ExplicitTimezoneFacet(ExplicitTimezoneMode.Optional),
            _ => null,
        };

    private static WhiteSpaceFacet? WhiteSpace(string value) =>
        value switch
        {
            "preserve" => new WhiteSpaceFacet(WhitespaceMode.Preserve),
            "replace" => new WhiteSpaceFacet(WhitespaceMode.Replace),
            "collapse" => new WhiteSpaceFacet(WhitespaceMode.Collapse),
            _ => null,
        };

    private static bool TryInt(string value, out int number)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number))
        {
            return true;
        }

        // A value beyond the range of an int still is a non-negative integer when it is all digits: it clamps.
        number = int.MaxValue;
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return value.Length > 0;
    }

    private static IFacet? Length(FacetDeclaration declaration, FacetBase? facts)
    {
        if (facts is null || !TryInt(declaration.Value, out var value))
        {
            return null;
        }

        if (facts.Primitive is PrimitiveKind.HexBinary or PrimitiveKind.Base64Binary)
        {
            var encoding = facts.Primitive == PrimitiveKind.HexBinary ? BinaryKind.HexBinary : BinaryKind.Base64Binary;
            return declaration.Kind switch
            {
                FacetKind.Length => BinaryLengthFacet.Length(value, encoding),
                FacetKind.MinLength => BinaryLengthFacet.MinLength(value, encoding),
                _ => BinaryLengthFacet.MaxLength(value, encoding),
            };
        }

        var unit = facts.Variety == DatatypeVariety.List ? LengthUnit.Items : LengthUnit.Characters;
        return declaration.Kind switch
        {
            FacetKind.Length => LengthFacet.Length(value, unit),
            FacetKind.MinLength => LengthFacet.MinLength(value, unit),
            _ => LengthFacet.MaxLength(value, unit),
        };
    }

    private static IFacet? Digits(FacetDeclaration declaration, FacetBase? facts)
    {
        if (
            facts?.Descriptor is not (IDatatypeDescriptor<XsdDecimal> or IDatatypeDescriptor<XsdInteger>)
            || !TryInt(declaration.Value, out var value)
        )
        {
            return null;
        }

        if (declaration.Kind == FacetKind.TotalDigits)
        {
            return value > 0 ? new TotalDigitsFacet(value) : null;
        }

        return new FractionDigitsFacet(value);
    }

    private static IFacet? Bound(FacetDeclaration declaration, FacetBase? facts) =>
        facts?.Descriptor switch
        {
            IDatatypeDescriptor<XsdDecimal> type => Bound(declaration, type),
            IDatatypeDescriptor<XsdInteger> type => Bound(declaration, type),
            IDatatypeDescriptor<float> type => Bound(declaration, type),
            IDatatypeDescriptor<double> type => Bound(declaration, type),
            IDatatypeDescriptor<XsdDuration> type => Bound(declaration, type),
            IDatatypeDescriptor<XsdDateTime> type => Bound(declaration, type),
            _ => null,
        };

    private static BoundFacet<TValue>? Bound<TValue>(FacetDeclaration declaration, IDatatypeDescriptor<TValue> type)
        where TValue : struct
    {
        var parsed = type.Parse(declaration.Value.AsSpan());
        return parsed.Success ? new BoundFacet<TValue>(declaration.Kind, type, parsed.Value) : null;
    }
}
