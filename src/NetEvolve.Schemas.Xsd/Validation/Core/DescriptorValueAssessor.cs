namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using DatatypeResult = NetEvolve.Schemas.Xml.Datatypes.Contracts.ValidationResult;

/// <summary>
/// The default <see cref="ISimpleValueAssessor"/>: it validates through the datatype descriptors of the Xml package.
/// </summary>
/// <remarks>
/// The facets a <see cref="SimpleTypeDefinition"/> adds are evaluated by its compiled validator; lists are assessed item by item
/// and unions by their first accepting member, then against their own facets. Limitations: a definition that wraps an unknown
/// datatype is accepted, and the facets that the validator cannot compile are not evaluated (see <see cref="SimpleTypeValidator"/>).
/// </remarks>
internal sealed class DescriptorValueAssessor : ISimpleValueAssessor
{
    private readonly Dictionary<string, IDatatypeDescriptor> _builtIns = new(StringComparer.Ordinal);

    internal DescriptorValueAssessor(XsdVersion version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        var types = XsdBuiltInTypes.For(version);
        for (var i = 0; i < types.Count; i++)
        {
            if (types[i] is XsdBuiltInType { Descriptor: { } descriptor } builtIn)
            {
                _builtIns[builtIn.Name.LocalName] = descriptor;
            }
        }
    }

    /// <inheritdoc />
    public DatatypeResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex)
    {
        ArgumentNullException.ThrowIfNull(simpleType);

        memberIndex = -1;
        switch (simpleType)
        {
            case XsdBuiltInType { Descriptor: { } descriptor }:
                return descriptor.Validate(lexical);
            case SimpleTypeDefinition definition:
                return AssessDefinition(definition, lexical, out memberIndex);
            default:
                return DatatypeResult.Valid;
        }
    }

    private DatatypeResult AssessDefinition(
        SimpleTypeDefinition definition,
        ReadOnlySpan<char> lexical,
        out int memberIndex
    )
    {
        memberIndex = -1;
        switch (definition.Variety)
        {
            case DatatypeVariety.List:
                return AssessOwn(definition, lexical, AssessList(definition.ItemType, lexical));
            case DatatypeVariety.Union:
                var union = AssessUnion(definition, lexical, out memberIndex);
                var own = AssessOwn(definition, lexical, union);
                memberIndex = own.IsValid ? memberIndex : -1;
                return own;
            default:
                if (definition.Validator is { } validator)
                {
                    return validator.Validate(lexical);
                }

                if (definition.BaseType is not null)
                {
                    return Assess(definition.BaseType, lexical, out memberIndex);
                }

                return
                    string.Equals(definition.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
                    && _builtIns.TryGetValue(definition.Name.LocalName, out var descriptor)
                    ? descriptor.Validate(lexical)
                    : DatatypeResult.Valid;
        }
    }

    private static DatatypeResult AssessOwn(
        SimpleTypeDefinition definition,
        ReadOnlySpan<char> lexical,
        DatatypeResult assessed
    ) => assessed.IsValid && definition.Validator is { } validator ? validator.Validate(lexical) : assessed;

    private DatatypeResult AssessList(SimpleTypeDefinition? itemType, ReadOnlySpan<char> lexical)
    {
        if (itemType is null)
        {
            return DatatypeResult.Valid;
        }

        var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
        while (cursor.TryNext(lexical, out var item))
        {
            if (!Assess(itemType, lexical.Slice(item.Offset, item.Length), out _).IsValid)
            {
                return DatatypeResult.Invalid(DatatypeError.InvalidListItem, FacetKind.None, item.Offset);
            }
        }

        return DatatypeResult.Valid;
    }

    private DatatypeResult AssessUnion(SimpleTypeDefinition union, ReadOnlySpan<char> lexical, out int memberIndex)
    {
        for (var i = 0; i < union.MemberTypes.Count; i++)
        {
            if (Assess(union.MemberTypes[i], lexical, out _).IsValid)
            {
                memberIndex = i;
                return DatatypeResult.Valid;
            }
        }

        memberIndex = -1;
        return DatatypeResult.Invalid(DatatypeError.NoUnionMember, FacetKind.None, 0);
    }
}
