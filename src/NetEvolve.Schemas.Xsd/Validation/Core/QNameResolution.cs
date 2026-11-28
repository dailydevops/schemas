namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Resolves the instance values of <c>xs:QName</c>, <c>xs:NOTATION</c>, and the types derived from them against the namespace
/// declarations in scope of the element or attribute (<c>cvc-resolve-instance</c>, Structures 1.0 section 3.15.4).
/// </summary>
/// <remarks>
/// The datatype descriptors check the lexical form only. Limitation: a NOTATION value is not checked against the declared
/// notations here, and a facet is not evaluated on the resolved value.
/// </remarks>
internal static class QNameResolution
{
    /// <summary>Checks that every QName in a value of the type has a bound prefix.</summary>
    /// <param name="type">The simple type of the value.</param>
    /// <param name="lexical">The lexical value as it appears in the instance.</param>
    /// <param name="context">The namespace declarations in scope.</param>
    /// <param name="assessor">The datatype contract, for union members that are no QName types.</param>
    /// <returns><see langword="false"/> when a prefix of the value is not bound.</returns>
    internal static bool Resolves(
        IXsdComponent type,
        ReadOnlySpan<char> lexical,
        INamespaceContext context,
        ISimpleValueAssessor assessor
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(assessor);

        if (!Involves(type))
        {
            return true;
        }

        if (type is not SimpleTypeDefinition definition)
        {
            return IsBound(lexical, context);
        }

        switch (definition.Variety)
        {
            case DatatypeVariety.List:
                var cursor = new XsdListCursor(new TextRange(0, lexical.Length));
                while (cursor.TryNext(lexical, out var item))
                {
                    if (!Resolves(definition.ItemType!, lexical.Slice(item.Offset, item.Length), context, assessor))
                    {
                        return false;
                    }
                }

                return true;
            case DatatypeVariety.Union:
                foreach (var member in definition.MemberTypes)
                {
                    if (
                        Involves(member)
                            ? Resolves(member, lexical, context, assessor)
                            : assessor.Assess(member, lexical, out _).IsValid
                    )
                    {
                        return true;
                    }
                }

                return false;
            default:
                return IsBound(lexical, context);
        }
    }

    // The type is, or derives from, QName or NOTATION, or it is a list or union that has such an item or member.
    private static bool Involves(IXsdComponent type)
    {
        if (type is not SimpleTypeDefinition definition)
        {
            return IsQNameType(type);
        }

        switch (definition.Variety)
        {
            case DatatypeVariety.List:
                return definition.ItemType is { } item && Involves(item);
            case DatatypeVariety.Union:
                foreach (var member in definition.MemberTypes)
                {
                    if (Involves(member))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return definition.BaseType is { } baseType ? Involves(baseType) : IsQNameType(definition);
        }
    }

    private static bool IsQNameType(IXsdComponent type) =>
        string.Equals(type.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
        && type.Name.LocalName is "QName" or "NOTATION";

    // The whitespace facet of QName is collapse; a lexical form that is malformed was reported by the datatype already.
    private static bool IsBound(ReadOnlySpan<char> lexical, INamespaceContext context)
    {
        var start = 0;
        var end = lexical.Length;
        while (start < end && IsSpace(lexical[start]))
        {
            start++;
        }

        while (end > start && IsSpace(lexical[end - 1]))
        {
            end--;
        }

        var trimmed = lexical.Slice(start, end - start);
        return XsdQName.Scan(trimmed, out _, out _) != DatatypeError.None
            || XsdQName.Parse(trimmed, context).Error != DatatypeError.UnboundPrefix;
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';
}
