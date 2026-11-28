namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Derives the typed value of a node from its simple type and its text (XDM, typed-value accessor; design #146 DP-18): the lexical mapping
/// of the type, or of the accepting member of a union, one item per token for a list, and the data model type of a type, which is its
/// nearest built-in ancestor because the data model knows no user-defined type.
/// </summary>
/// <remarks>
/// <para>
/// The text is converted by casting an <c>xs:untypedAtomic</c> item to the built-in type through the casting table of the XPath package,
/// which also applies the white space handling of that type. A value that the cast rejects, and a type without any built-in ancestor, keep
/// the value as <c>xs:untypedAtomic</c>.
/// </para>
/// <para>
/// Limitations: the white space facet of a user-defined type is not applied, only that of its built-in ancestor; a value of <c>xs:QName</c>
/// or <c>xs:NOTATION</c> needs the namespace context of the node, so it stays <c>xs:untypedAtomic</c> instead of resolving an unprefixed name
/// against no namespace (#604).
/// </para>
/// </remarks>
internal static class TypedValues
{
    /// <summary>Gets the handle of <c>xs:anyType</c>.</summary>
    internal static readonly XdmTypeId AnyType = XdmTypes.Get("anyType");

    /// <summary>Gets the handle of <c>xs:anySimpleType</c>.</summary>
    internal static readonly XdmTypeId AnySimpleType = XdmTypes.Get("anySimpleType");

    /// <summary>Gets the simple type that gives the typed value: the type itself, or the simple content type of a complex type.</summary>
    /// <param name="type">The type of an element or attribute.</param>
    /// <returns>The simple type.</returns>
    internal static IXsdComponent SimpleTypeOf(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type is ComplexTypeDefinition complex ? complex.SimpleContentType! : type;
    }

    /// <summary>Gets the data model type of a type: the nearest built-in ancestor.</summary>
    /// <param name="type">The type of an element or attribute.</param>
    /// <returns>The handle: a built-in simple type, <c>xs:anySimpleType</c> for a list, a union, or a type without a built-in ancestor, and <c>xs:anyType</c> for a complex type without simple content.</returns>
    internal static XdmTypeId DataModelType(IXsdComponent type)
    {
        ArgumentNullException.ThrowIfNull(type);

        while (true)
        {
            switch (type)
            {
                case ComplexTypeDefinition { ContentKind: ComplexContentKind.Simple } complex:
                    type = complex.SimpleContentType!;
                    continue;
                case ComplexTypeDefinition:
                    return AnyType;
                case SimpleTypeDefinition definition:
                    if (
                        IsSchemaType(definition.Name)
                        && XdmTypes.TryGet(SchemaGrammar.Namespace, definition.Name.LocalName, out var id)
                    )
                    {
                        return id;
                    }

                    if (definition.Variety == DatatypeVariety.Atomic && definition.BaseType is { } baseType)
                    {
                        type = baseType;
                        continue;
                    }

                    return AnySimpleType;
                default:
                    _ = XdmTypes.TryGet(SchemaGrammar.Namespace, type.Name.LocalName, out var builtIn);
                    return builtIn;
            }
        }
    }

    /// <summary>Appends the items of the typed value of a simple type.</summary>
    /// <param name="type">The simple type: a built-in type or a simple type definition.</param>
    /// <param name="text">The text of the node.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="items">The list that receives the items.</param>
    internal static void Append(IXsdComponent type, string text, ISimpleValueAssessor assessor, List<XdmItem> items)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(items);

        switch (type)
        {
            case SimpleTypeDefinition definition:
                AppendDefinition(definition, text, assessor, items);
                break;
            case XsdBuiltInType { Descriptor.Variety: DatatypeVariety.List } list:
                AppendBuiltInList(list.Name.LocalName, text, items);
                break;
            default:
                AppendBuiltIn(type.Name.LocalName, text, items);
                break;
        }
    }

    private static void AppendDefinition(
        SimpleTypeDefinition definition,
        string text,
        ISimpleValueAssessor assessor,
        List<XdmItem> items
    )
    {
        switch (definition.Variety)
        {
            case DatatypeVariety.List when definition.ItemType is { } itemType:
                AppendTokens(itemType, text, assessor, items);
                break;
            case DatatypeVariety.List:
                // A definition that wraps a built-in list datatype has no item type component.
                AppendBuiltInList(definition.Name.LocalName, text, items);
                break;
            case DatatypeVariety.Union:
                // The accepting member decides, as the assessment of the value did.
                _ = assessor.Assess(definition, text.AsSpan(), out var member);
                if (member < 0)
                {
                    items.Add(XdmItem.FromUntypedAtomic(text));
                }
                else
                {
                    Append(definition.MemberTypes[member], text, assessor, items);
                }

                break;
            default:
                if (IsSchemaType(definition.Name))
                {
                    AppendBuiltIn(definition.Name.LocalName, text, items);
                }
                else if (definition.BaseType is { } baseType)
                {
                    Append(baseType, text, assessor, items);
                }
                else
                {
                    items.Add(XdmItem.FromUntypedAtomic(text));
                }

                break;
        }
    }

    private static void AppendTokens(
        IXsdComponent itemType,
        string text,
        ISimpleValueAssessor assessor,
        List<XdmItem> items
    )
    {
        var cursor = new XsdListCursor(new TextRange(0, text.Length));
        while (cursor.TryNext(text.AsSpan(), out var token))
        {
            Append(itemType, text.Substring(token.Offset, token.Length), assessor, items);
        }
    }

    // The three built-in list types of the schema namespace have one item type each.
    private static void AppendBuiltInList(string name, string text, List<XdmItem> items)
    {
        var itemName = name switch
        {
            "NMTOKENS" => "NMTOKEN",
            "IDREFS" => "IDREF",
            "ENTITIES" => "ENTITY",
            _ => null,
        };
        if (itemName is null)
        {
            items.Add(XdmItem.FromUntypedAtomic(text));
            return;
        }

        var cursor = new XsdListCursor(new TextRange(0, text.Length));
        while (cursor.TryNext(text.AsSpan(), out var token))
        {
            AppendBuiltIn(itemName, text.Substring(token.Offset, token.Length), items);
        }
    }

    // Casts to the built-in type. Only a type that has no row in the casting table (the abstract xs:NOTATION) takes the
    // row of its nearest ancestor; a cast that fails, and a type that needs the namespaces of the node (xs:QName), keep the untyped value.
    private static void AppendBuiltIn(string localName, string text, List<XdmItem> items)
    {
        _ = XdmTypes.TryGet(SchemaGrammar.Namespace, localName, out var target);
        var untyped = XdmItem.FromUntypedAtomic(text);
        var table = CastingCatalog.Default.Table;
        while (target.Value > 0 && !table.IsTarget(target))
        {
            target = XdmTypes.ParentOf(target);
        }

        if (
            target.Value > 0
            && !table.IsNamespaceSensitive(target)
            && table.TryCast(untyped, target, out var item) == CastStatus.Ok
        )
        {
            items.Add(item);
        }
        else
        {
            items.Add(untyped);
        }
    }

    private static bool IsSchemaType(XPathQName name) =>
        string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);
}
