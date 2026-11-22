namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>A union datatype: the first member type, in declaration order, that accepts the lexical form wins (XSD 1.1 Part 2, section 2.5.1.3).</summary>
/// <remarks>Limitations: union facets are DT-13; <see cref="BaseType"/> is <see langword="null"/>; the union is ordered only when every member is.</remarks>
public sealed class UnionDescriptor : IDatatypeDescriptor
{
    /// <summary>Initializes a new instance of the <see cref="UnionDescriptor"/> class.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="members">The member types in declaration order; at least one.</param>
    public UnionDescriptor(string name, IReadOnlyList<IDatatypeDescriptor> members)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentOutOfRangeException.ThrowIfLessThan(members.Count, 1);
        Name = name;
        Members = members;
        LexicalMode = members[0].LexicalMode;
        IsOrdered = true;
        for (var i = 0; i < members.Count; i++)
        {
            IsOrdered &= members[i].IsOrdered;
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public DatatypeVariety Variety => DatatypeVariety.Union;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType => null;

    /// <inheritdoc />
    public XsdVersion LexicalMode { get; }

    /// <inheritdoc />
    public bool IsOrdered { get; }

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets { get; } = [];

    /// <summary>Gets the member types in declaration order.</summary>
    public IReadOnlyList<IDatatypeDescriptor> Members { get; }

    /// <summary>Finds the first member that accepts a lexical form.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <param name="memberIndex">The zero-based index of the member, -1 when none accepts.</param>
    /// <returns><see langword="true"/> when a member accepts the form.</returns>
    public bool TryGetMemberIndex(ReadOnlySpan<char> lexical, out int memberIndex)
    {
        for (var i = 0; i < Members.Count; i++)
        {
            if (Members[i].Validate(lexical).IsValid)
            {
                memberIndex = i;
                return true;
            }
        }

        memberIndex = -1;
        return false;
    }

    /// <inheritdoc />
    public ValidationResult Validate(ReadOnlySpan<char> lexical) =>
        TryGetMemberIndex(lexical, out _)
            ? ValidationResult.Valid
            : ValidationResult.Invalid(DatatypeError.NoUnionMember, FacetKind.None, 0);

    /// <summary>Parses a lexical form.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns>The union value, or <see cref="DatatypeError.NoUnionMember"/>.</returns>
    public ParseResult<XsdUnion> Parse(ReadOnlySpan<char> lexical) =>
        TryGetMemberIndex(lexical, out var index)
            ? new ParseResult<XsdUnion>(new XsdUnion(new TextRange(0, lexical.Length), index))
            : new ParseResult<XsdUnion>(DatatypeError.NoUnionMember, 0);
}
