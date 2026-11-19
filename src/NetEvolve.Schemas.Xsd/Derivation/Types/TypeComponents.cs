namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>What the derivation checks need to know about a type component, whichever form it takes.</summary>
/// <remarks>
/// A type is represented by a <see cref="ComplexTypeDefinition"/>, a <see cref="SimpleTypeDefinition"/>, or the registered
/// <see cref="XsdBuiltInType"/>, and a built-in type reaches a schema as a wrapper of its datatype that is a new object on every
/// use. Two named type components therefore are the same type definition when their expanded names are equal (a symbol space
/// holds one type per name); anonymous ones only by reference.
/// </remarks>
internal static class TypeComponents
{
    private const string UrTypeName = "anyType";

    /// <summary>Determines whether two type components are the same type definition.</summary>
    /// <param name="left">The first type.</param>
    /// <param name="right">The second type.</param>
    /// <returns><see langword="true"/> when they are the same type definition.</returns>
    internal static bool IsSame(IXsdComponent left, IXsdComponent right)
    {
        Argument.ThrowIfNull(left);
        Argument.ThrowIfNull(right);

        return ReferenceEquals(left, right) || (left.Name != default && left.Name == right.Name);
    }

    /// <summary>Determines whether a type component is the ur-type <c>xs:anyType</c>, which has two forms.</summary>
    /// <param name="type">The type.</param>
    /// <returns><see langword="true"/> for the ur-type.</returns>
    internal static bool IsUrType(IXsdComponent type)
    {
        Argument.ThrowIfNull(type);

        return type.Kind == XsdComponentKind.ComplexType
            && string.Equals(type.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            && string.Equals(type.Name.LocalName, UrTypeName, StringComparison.Ordinal);
    }

    /// <summary>Gets the derivations that are not allowed from a type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The final set, empty for a built-in type.</returns>
    internal static XsdDerivationMethod FinalOf(IXsdComponent type)
    {
        Argument.ThrowIfNull(type);

        return type switch
        {
            ComplexTypeDefinition complex => complex.Final,
            SimpleTypeDefinition simple => simple.Final,
            _ => XsdDerivationMethod.None,
        };
    }

    /// <summary>Views a type component as a complex type definition; the registered ur-type is <see cref="ComplexTypeDefinition.AnyType"/>.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The complex type definition, <see langword="null"/> for a simple type.</returns>
    internal static ComplexTypeDefinition? AsComplex(IXsdComponent type)
    {
        Argument.ThrowIfNull(type);

        return type switch
        {
            ComplexTypeDefinition complex => complex,
            XsdBuiltInType { Kind: XsdComponentKind.ComplexType } => ComplexTypeDefinition.AnyType,
            _ => null,
        };
    }

    /// <summary>Indexes attribute uses by expanded name.</summary>
    /// <param name="uses">The uses.</param>
    /// <returns>The uses by the name of their declaration.</returns>
    internal static Dictionary<XPathQName, AttributeUse> IndexUses(IReadOnlyList<AttributeUse> uses)
    {
        Argument.ThrowIfNull(uses);

        var index = new Dictionary<XPathQName, AttributeUse>(uses.Count);
        for (var i = 0; i < uses.Count; i++)
        {
            index[uses[i].Name] = uses[i];
        }

        return index;
    }

    /// <summary>Names a type for a message.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The quoted name, or a note for an anonymous type.</returns>
    internal static string Describe(IXsdComponent type)
    {
        Argument.ThrowIfNull(type);

        return type.Name == default ? "an anonymous type" : "'" + type.Name.LocalName + "'";
    }
}
