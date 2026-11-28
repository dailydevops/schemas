namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A list datatype: a whitespace-separated sequence of values of one item type (XSD 1.1 Part 2, section 2.5.1.2).</summary>
/// <remarks>
/// Limitations: list facets (length, minLength, maxLength, enumeration, pattern) are DT-13; <see cref="BaseType"/> is
/// <see langword="null"/> because the list is not derived from a registered base; list values do not support identity or order.
/// </remarks>
public sealed class ListDescriptor : IDatatypeDescriptor
{
    /// <summary>Initializes a new instance of the <see cref="ListDescriptor"/> class.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="itemType">The type of the items.</param>
    public ListDescriptor(string name, IDatatypeDescriptor itemType)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(itemType);
        Name = name;
        ItemType = itemType;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public DatatypeVariety Variety => DatatypeVariety.List;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType => null;

    /// <inheritdoc />
    public XsdVersion LexicalMode => ItemType.LexicalMode;

    /// <inheritdoc />
    public bool IsOrdered => false;

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets { get; } = [];

    /// <summary>Gets the type of the items.</summary>
    public IDatatypeDescriptor ItemType { get; }

    /// <summary>Gets the whitespace mode; lists always collapse.</summary>
    public WhitespaceMode Whitespace { get; } = WhitespaceMode.Collapse;

    /// <inheritdoc />
    public ValidationResult Validate(ReadOnlySpan<char> lexical) => Validate(lexical, out _);

    /// <summary>Validates a lexical form and reports which item failed.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="itemIndex">The zero-based index of the failing item, -1 when valid.</param>
    /// <returns>The result; the offset is relative to <paramref name="lexical"/>.</returns>
    public ValidationResult Validate(ReadOnlySpan<char> lexical, out int itemIndex)
    {
        var failure = Check(lexical, out itemIndex, out _);
        return failure.IsValid
            ? ValidationResult.Valid
            : ValidationResult.Invalid(DatatypeError.InvalidListItem, failure.FailedFacet, failure.Offset);
    }

    /// <summary>Parses a lexical form.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The list value, or <see cref="DatatypeError.InvalidListItem"/> with the offset of the failure.</returns>
    public ParseResult<XsdList> Parse(ReadOnlySpan<char> lexical) => Parse(lexical, out _);

    /// <summary>Parses a lexical form and reports which item failed.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="itemIndex">The zero-based index of the failing item, -1 on success.</param>
    /// <returns>The list value, or <see cref="DatatypeError.InvalidListItem"/> with the offset of the failure.</returns>
    public ParseResult<XsdList> Parse(ReadOnlySpan<char> lexical, out int itemIndex)
    {
        var failure = Check(lexical, out itemIndex, out var count);
        return failure.IsValid
            ? new ParseResult<XsdList>(new XsdList(new TextRange(0, lexical.Length), count))
            : new ParseResult<XsdList>(DatatypeError.InvalidListItem, failure.Offset);
    }

    private ValidationResult Check(ReadOnlySpan<char> lexical, out int itemIndex, out int count)
    {
        var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
        count = 0;
        while (cursor.TryNext(lexical, out var item))
        {
            var result = ItemType.Validate(item.Slice(lexical));
            if (!result.IsValid)
            {
                itemIndex = count;
                return ValidationResult.Invalid(result.Error, result.FailedFacet, item.Offset + result.Offset);
            }

            count++;
        }

        itemIndex = -1;
        return ValidationResult.Valid;
    }
}
