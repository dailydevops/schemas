namespace NetEvolve.Schemas.Xml.Datatypes.Names;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

/// <summary>
/// A list type derived with <c>minLength</c> 1, as IDREFS, ENTITIES, and NMTOKENS are (XSD 1.1 Part 2, sections 3.4.9, 3.4.11,
/// and 3.4.12): the items are validated by a <see cref="ListDescriptor"/>, and an empty list is rejected first.
/// </summary>
/// <remarks>Limitation: the facets of further restrictions of these list types are DT-13; <see cref="BaseType"/> is <see langword="null"/>, like on <see cref="ListDescriptor"/>.</remarks>
public sealed class NameListDescriptor : IDatatypeDescriptor
{
    private readonly ListDescriptor _list;
    private readonly LengthFacet _minLength = LengthFacet.MinLength(1, LengthUnit.Items);

    /// <summary>Initializes a new instance of the <see cref="NameListDescriptor"/> class.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="itemType">The type of the items.</param>
    public NameListDescriptor(string name, IDatatypeDescriptor itemType)
    {
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(itemType);
        _list = new ListDescriptor(name, itemType);
        Facets = [_minLength];
    }

    /// <inheritdoc />
    public string Name => _list.Name;

    /// <inheritdoc />
    public DatatypeVariety Variety => DatatypeVariety.List;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType => null;

    /// <inheritdoc />
    public XsdVersion LexicalMode => _list.LexicalMode;

    /// <inheritdoc />
    public bool IsOrdered => false;

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets { get; }

    /// <summary>Gets the type of the items.</summary>
    public IDatatypeDescriptor ItemType => _list.ItemType;

    /// <inheritdoc />
    public ValidationResult Validate(ReadOnlySpan<char> lexical) => Validate(lexical, out _);

    /// <summary>Validates a lexical form and reports which item failed.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="itemIndex">The zero-based index of the failing item, -1 when valid or when the list is empty.</param>
    /// <returns>The result; the offset is relative to <paramref name="lexical"/>.</returns>
    public ValidationResult Validate(ReadOnlySpan<char> lexical, out int itemIndex)
    {
        if (!_minLength.IsSatisfiedBy(lexical))
        {
            itemIndex = -1;
            return ValidationResult.Invalid(DatatypeError.None, FacetKind.MinLength, 0);
        }

        return _list.Validate(lexical, out itemIndex);
    }

    /// <summary>Parses a lexical form.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The list value, or the error with the offset of the failure.</returns>
    public ParseResult<XsdList> Parse(ReadOnlySpan<char> lexical) => Parse(lexical, out _);

    /// <summary>Parses a lexical form and reports which item failed.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="itemIndex">The zero-based index of the failing item, -1 on success or when the list is empty.</param>
    /// <returns>The list value, or <see cref="DatatypeError.FacetViolation"/> for an empty list, or <see cref="DatatypeError.InvalidListItem"/> with the offset of the failure.</returns>
    public ParseResult<XsdList> Parse(ReadOnlySpan<char> lexical, out int itemIndex)
    {
        if (!_minLength.IsSatisfiedBy(lexical))
        {
            itemIndex = -1;
            return new ParseResult<XsdList>(DatatypeError.FacetViolation, 0);
        }

        return _list.Parse(lexical, out itemIndex);
    }
}
