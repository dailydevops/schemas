namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// What the facet check needs to know about a base type: its variety, its nearest built-in datatype, its primitive family, and the
/// effective facets that the base type carries.
/// </summary>
/// <remarks>
/// <para>
/// The family comes from the root of the base type chain, which is a built-in type of the Xml package. A list or union has no
/// primitive and no datatype; its facets are the ones the definition lists.
/// </para>
/// <para>
/// Limitations: the facets of the built-in integer types (for example the range of the built-in byte type) are internal to the Xml package and
/// cannot be read, so the check of a bound against the base of such a type goes through the datatype, which rejects a value outside
/// its range; the other facets of the built-in types are read from the facet list. Facets that no check models, such as
/// <c>assertions</c>, are ignored. The type system fixes <c>whiteSpace</c> for every family but strings and <c>fractionDigits</c> for
/// the integer family; these two flags are seeded here because the datatype contract does not carry flags.
/// </para>
/// </remarks>
internal sealed class FacetBase
{
    private readonly IDatatypeDescriptor? _descriptor;
    private readonly IReadOnlyList<IFacet> _facets;

    private FacetBase(
        DatatypeVariety variety,
        IDatatypeDescriptor? descriptor,
        PrimitiveKind primitive,
        IReadOnlyList<IFacet> facets
    )
    {
        Variety = variety;
        _descriptor = descriptor;
        Primitive = primitive;
        _facets = facets;
    }

    /// <summary>Gets the variety.</summary>
    public DatatypeVariety Variety { get; }

    /// <summary>Gets the primitive family, <see cref="PrimitiveKind.None"/> for lists and unions.</summary>
    public PrimitiveKind Primitive { get; }

    /// <summary>Gets the nearest built-in datatype, <see langword="null"/> for a list or union definition.</summary>
    public IDatatypeDescriptor? Descriptor => _descriptor;

    /// <summary>Gets a value indicating whether the base type already carries an <c>enumeration</c> facet.</summary>
    public bool HasEnumeration
    {
        get
        {
            for (var i = 0; i < _facets.Count; i++)
            {
                if (_facets[i].Kind == FacetKind.Enumeration)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Describes a base type.</summary>
    /// <param name="baseType">The base type.</param>
    /// <param name="version">The XSD version.</param>
    /// <returns>The description, <see langword="null"/> when the base is not a simple type that the check can see through.</returns>
    internal static FacetBase? Resolve(IXsdComponent baseType, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        return baseType switch
        {
            XsdBuiltInType { Descriptor: { } descriptor } => FromDescriptor(descriptor, descriptor.Facets),
            SimpleTypeDefinition definition => FromDefinition(definition, version),
            _ => null,
        };
    }

    /// <summary>Seeds the facets that the type system fixes for this family.</summary>
    /// <param name="inherited">The facets that the base type fixes.</param>
    /// <returns>The union of the inherited facets and the fixed facets of the family.</returns>
    internal FacetKindSet SeedFixed(FacetKindSet inherited)
    {
        var fixedFacets = inherited;
        if (Variety != DatatypeVariety.Union && Primitive != PrimitiveKind.StringType)
        {
            fixedFacets = fixedFacets.With(FacetKind.WhiteSpace);
        }

        return _descriptor is IDatatypeDescriptor<XsdInteger>
            ? fixedFacets.With(FacetKind.FractionDigits)
            : fixedFacets;
    }

    /// <summary>Reads the effective length and whitespace facets of the base type.</summary>
    /// <param name="fixedFacets">The facets that the base type fixes.</param>
    /// <returns>The facets of the base type.</returns>
    internal FacetSet CoreSet(FacetKindSet fixedFacets)
    {
        int? length = null;
        int? minLength = null;
        int? maxLength = null;
        var whiteSpace = DefaultWhiteSpace();
        for (var i = 0; i < _facets.Count; i++)
        {
            switch (_facets[i])
            {
                case LengthFacet facet:
                    Take(facet.Kind, facet.Value, ref length, ref minLength, ref maxLength);
                    break;
                case BinaryLengthFacet facet:
                    Take(facet.Kind, facet.Value, ref length, ref minLength, ref maxLength);
                    break;
                case WhiteSpaceFacet facet:
                    whiteSpace = facet.Mode;
                    break;
            }
        }

        return new FacetSet(length, minLength, maxLength, whiteSpace, fixedFacets);
    }

    /// <summary>Reads the effective bounds and digits facets of the base type.</summary>
    /// <typeparam name="TValue">The value type of the bounds.</typeparam>
    /// <param name="fixedFacets">The facets that the base type fixes.</param>
    /// <returns>The facets of the base type.</returns>
    internal OrderedFacetSet<TValue> OrderedSet<TValue>(FacetKindSet fixedFacets)
        where TValue : struct
    {
        TValue? minInclusive = null;
        TValue? minExclusive = null;
        TValue? maxInclusive = null;
        TValue? maxExclusive = null;
        int? totalDigits = null;

        // The integer family fixes fractionDigits at zero, which the datatype contract does not list.
        int? fractionDigits = _descriptor is IDatatypeDescriptor<XsdInteger> ? 0 : null;
        for (var i = 0; i < _facets.Count; i++)
        {
            switch (_facets[i])
            {
                case BoundFacet<TValue> { Kind: FacetKind.MinInclusive } bound:
                    minInclusive = bound.Bound;
                    break;
                case BoundFacet<TValue> { Kind: FacetKind.MinExclusive } bound:
                    minExclusive = bound.Bound;
                    break;
                case BoundFacet<TValue> { Kind: FacetKind.MaxInclusive } bound:
                    maxInclusive = bound.Bound;
                    break;
                case BoundFacet<TValue> bound:
                    maxExclusive = bound.Bound;
                    break;
                case TotalDigitsFacet digits:
                    totalDigits = digits.Value;
                    break;
                case FractionDigitsFacet digits:
                    fractionDigits = digits.Value;
                    break;
            }
        }

        return new OrderedFacetSet<TValue>(
            minInclusive,
            minExclusive,
            maxInclusive,
            maxExclusive,
            totalDigits,
            fractionDigits,
            null,
            fixedFacets
        );
    }

    private static FacetBase? FromDescriptor(IDatatypeDescriptor descriptor, IReadOnlyList<IFacet> facets)
    {
        if (descriptor.Variety != DatatypeVariety.Atomic)
        {
            return new FacetBase(descriptor.Variety, descriptor, PrimitiveKind.None, facets);
        }

        var primitive = PrimitiveOf(descriptor);

        // anySimpleType and anyAtomicType have no primitive; restricting them is not a facet restriction of a family.
        return primitive == PrimitiveKind.None
            ? null
            : new FacetBase(DatatypeVariety.Atomic, descriptor, primitive, facets);
    }

    private static FacetBase? FromDefinition(SimpleTypeDefinition definition, XsdVersion version)
    {
        if (definition.Variety != DatatypeVariety.Atomic)
        {
            return new FacetBase(definition.Variety, null, PrimitiveKind.None, definition.Facets);
        }

        var root = definition;
        while (root.BaseType is { } parent)
        {
            root = parent;
        }

        return
            string.Equals(root.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && BuiltInDatatypes.For(version).TryGet(root.Name.LocalName, out var descriptor)
            && descriptor is not null
            ? FromDescriptor(descriptor, definition.Facets)
            : null;
    }

    private static PrimitiveKind PrimitiveOf(IDatatypeDescriptor descriptor)
    {
        var root = descriptor;
        while (root.BaseType is { } parent)
        {
            root = parent;
        }

        return root.Name switch
        {
            "string" => PrimitiveKind.StringType,
            "boolean" => PrimitiveKind.Boolean,
            "decimal" => PrimitiveKind.DecimalType,
            "float" => PrimitiveKind.FloatType,
            "double" => PrimitiveKind.DoubleType,
            "duration" => PrimitiveKind.Duration,
            "dateTime" => PrimitiveKind.DateTime,
            "time" => PrimitiveKind.Time,
            "date" => PrimitiveKind.Date,
            "gYearMonth" => PrimitiveKind.GYearMonth,
            "gYear" => PrimitiveKind.GYear,
            "gMonthDay" => PrimitiveKind.GMonthDay,
            "gDay" => PrimitiveKind.GDay,
            "gMonth" => PrimitiveKind.GMonth,
            "hexBinary" => PrimitiveKind.HexBinary,
            "base64Binary" => PrimitiveKind.Base64Binary,
            "anyURI" => PrimitiveKind.AnyUri,
            "QName" => PrimitiveKind.QName,
            "NOTATION" => PrimitiveKind.Notation,
            _ => PrimitiveKind.None,
        };
    }

    private static void Take(FacetKind kind, int value, ref int? length, ref int? minLength, ref int? maxLength)
    {
        switch (kind)
        {
            case FacetKind.Length:
                length = value;
                break;
            case FacetKind.MinLength:
                minLength = value;
                break;
            default:
                maxLength = value;
                break;
        }
    }

    // The mode that the datatype applies when no whiteSpace facet says otherwise (Datatypes section 4.3.6).
    private WhitespaceMode DefaultWhiteSpace() =>
        _descriptor?.Name switch
        {
            "string" => WhitespaceMode.Preserve,
            "normalizedString" => WhitespaceMode.Replace,
            _ => WhitespaceMode.Collapse,
        };
}
