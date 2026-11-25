namespace NetEvolve.Schemas.XPath.Types;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// An item type bound against a static context: <c>item()</c>, an atomic type, or a kind test (XPath 2.0, 2.5.3 and 2.5.4).
/// An immutable struct; matching an item does not allocate.
/// </summary>
/// <remarks>
/// Limitations: <c>schema-element()</c> and <c>schema-attribute()</c> and types that are not built in need schema import (DEF-40, #52);
/// the nillable marker of <c>element(N, T?)</c> is accepted but has no effect because nodes are never nilled until the nilled
/// property exists; map and array tests belong to XP-32 and XP-69. A function test matches a function item by the signature of the item (see <see cref="FunctionSignature"/>).
/// </remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ItemTypeMatcher
{
    private readonly ItemTypeKind _kind;
    private readonly XPathNodeTest _test;
    private readonly XdmTypeId _type;
    private readonly FunctionSignature? _function;

    private ItemTypeMatcher(ItemTypeKind kind, XPathNodeTest test, XdmTypeId type, FunctionSignature? function = null)
    {
        _kind = kind;
        _test = test;
        _type = type;
        _function = function;
    }

    /// <summary>Gets a value indicating whether every item matches.</summary>
    public bool IsAnyItem => _kind == ItemTypeKind.AnyItem;

    /// <summary>Gets the type of an atomic type, or the type annotation that an element or attribute test names; <see langword="default"/> when there is none.</summary>
    public XdmTypeId Type => _type;

    /// <summary>Gets the signature of a typed function test; <see langword="null"/> for <c>function(*)</c> and for every other matcher.</summary>
    public FunctionSignature? FunctionSignature => _function;

    /// <summary>Binds an item type.</summary>
    /// <param name="itemType">The syntax of the item type.</param>
    /// <param name="context">The static context that resolves prefixes and the default namespace.</param>
    /// <returns>The matcher.</returns>
    /// <exception cref="XPathException">A prefix is not bound (XPST0081), a type name is not a built-in type (XPST0051), or a schema declaration is named (XPST0008).</exception>
    public static ItemTypeMatcher Bind(ItemType itemType, XPathStaticContext context)
    {
        ArgumentNullException.ThrowIfNull(itemType);
        ArgumentNullException.ThrowIfNull(context);

        switch (itemType)
        {
            case AnyItemType:
                return new(ItemTypeKind.AnyItem, default, default);
            case AtomicType atomic:
                return new(ItemTypeKind.Atomic, default, ResolveAtomic(atomic.Name, context));
            case KindItemType kind:
                return BindKindTest(kind.Test, context);
            case ParenthesizedItemType parenthesized:
                return Bind(parenthesized.Inner, context);
            case AnyFunctionTest:
                return new(ItemTypeKind.Function, default, default);
            case TypedFunctionTest typed:
                return new(ItemTypeKind.Function, default, default, BindSignature(typed, context));
            default:
                throw XPathErrors.Exception(
                    XPathErrorCodes.XPST0003,
                    itemType.Location.Offset,
                    itemType.Location.Length
                );
        }
    }

    /// <summary>Determines whether an item matches.</summary>
    /// <param name="item">The item.</param>
    /// <param name="annotations">The slot that supplies the type annotation of a node.</param>
    /// <param name="tree">The tree the cached matcher was bound for, updated when the item belongs to another tree.</param>
    /// <param name="bound">The name test bound for <paramref name="tree"/>.</param>
    /// <returns><see langword="true"/> when the item matches.</returns>
    public bool Matches(
        in XdmItem item,
        IXdmTypeAnnotationSource annotations,
        ref XmlTree? tree,
        ref XPathNodeMatcher bound
    )
    {
        ArgumentNullException.ThrowIfNull(annotations);

        switch (_kind)
        {
            case ItemTypeKind.AnyItem:
                return true;
            case ItemTypeKind.Atomic:
                return !item.IsNode && item.Kind != XdmItemKind.Function && XdmTypes.IsDerivedFrom(item.TypeId, _type);
            case ItemTypeKind.Function:
                return item.TryGetFunction(out var function)
                    && (_function is null || function!.Signature.IsSubtypeOf(_function));
            default:
                break;
        }

        if (!item.TryGetNode(out var node))
        {
            return false;
        }

        if (_kind == ItemTypeKind.AnyNode)
        {
            return true;
        }

        BindTo(node, ref tree, ref bound);
        if (_kind == ItemTypeKind.DocumentElement)
        {
            return node.Kind == XdmNodeKind.Document && MatchesDocumentElement(node, annotations, bound);
        }

        return bound.Matches(node) && MatchesType(node, annotations);
    }

    private static FunctionSignature BindSignature(TypedFunctionTest test, XPathStaticContext context)
    {
        var parameters = new SequenceTypeInfo[test.ParameterTypes.Count];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameters[i] = SequenceTypeInfo.Bind(test.ParameterTypes[i], context);
        }

        return new FunctionSignature(parameters, SequenceTypeInfo.Bind(test.ReturnType, context));
    }

    private static ItemTypeMatcher BindKindTest(KindTest test, XPathStaticContext context)
    {
        switch (test)
        {
            case SchemaElementTest or SchemaAttributeTest:
                throw XPathErrors.Exception(XPathErrorCodes.XPST0008, test.Location.Offset, test.Location.Length);
            case DocumentTest { Element: { } element }:
                var (documentElement, type) = BindNamedTest(element, context);
                return new(ItemTypeKind.DocumentElement, documentElement, type);
            case ElementTest or AttributeTest:
                var (named, annotation) = BindNamedTest(test, context);
                return new(ItemTypeKind.Node, named, annotation);
            case AnyKindTest:
                return new(ItemTypeKind.AnyNode, default, default);
            default:
                // text(), comment(), processing-instruction(), and document-node() carry no name or type.
                return new(ItemTypeKind.Node, ToNodeTest(test, context), default);
        }
    }

    // An element or attribute test: the name part is the existing node test, the type name is bound here.
    private static (XPathNodeTest Test, XdmTypeId Type) BindNamedTest(KindTest test, XPathStaticContext context)
    {
        switch (test)
        {
            case SchemaElementTest or SchemaAttributeTest:
                throw XPathErrors.Exception(XPathErrorCodes.XPST0008, test.Location.Offset, test.Location.Length);
            case ElementTest element:
                var elementType = element.TypeName is null ? default : ResolveAny(element.TypeName, context);
                var withoutType = new ElementTest(element.Location, element.Name, null, false);
                return (ToNodeTest(withoutType, context), elementType);
            case AttributeTest attribute:
                var attributeType = attribute.TypeName is null ? default : ResolveAny(attribute.TypeName, context);
                var plain = new AttributeTest(attribute.Location, attribute.Name, null);
                return (ToNodeTest(plain, context), attributeType);
            default:
                return (ToNodeTest(test, context), default);
        }
    }

    private static XPathNodeTest ToNodeTest(NodeTest test, XPathStaticContext context) =>
        XPathNodeTest.TryFromSyntax(test, Axis.Child, context, out var result)
            ? result
            : throw XPathErrors.Exception(XPathErrorCodes.XPST0003, test.Location.Offset, test.Location.Length);

    // The name of an atomic type must be an atomic built-in type.
    private static XdmTypeId ResolveAtomic(QualifiedName name, XPathStaticContext context)
    {
        var id = ResolveAny(name, context);
        return XdmTypes.IsAtomic(id) ? id : throw UnknownType(name);
    }

    // Any built-in type: the type name of an element or attribute test may also be xs:anyType, xs:anySimpleType, or xs:untyped.
    private static XdmTypeId ResolveAny(QualifiedName name, XPathStaticContext context)
    {
        string namespaceUri;
        if (name.Prefix is null)
        {
            namespaceUri = context.DefaultElementNamespace ?? string.Empty;
        }
        else if (!context.TryResolveNamespace(name.Prefix, out var resolved))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }
        else
        {
            namespaceUri = resolved ?? string.Empty;
        }

        // xs:dateTimeStamp belongs to XSD 1.1, which XPath 3.1 adds; before that it is not an in-scope type.
        return
            XdmTypes.TryGet(namespaceUri, name.LocalName, out var id)
            && (context.Version == XPathVersion.V31 || !IsDateTimeStamp(name.LocalName))
            ? id
            : throw UnknownType(name);
    }

    private static bool IsDateTimeStamp(string localName) =>
        string.Equals(localName, "dateTimeStamp", StringComparison.Ordinal);

    private static XPathException UnknownType(QualifiedName name) =>
        XPathErrors.Exception(XPathErrorCodes.XPST0051, name.Location.Offset, name.Location.Length);

    private void BindTo(in XPathNode node, ref XmlTree? tree, ref XPathNodeMatcher bound)
    {
        var current = node.Tree;
        if (!ReferenceEquals(current, tree))
        {
            tree = current;
            bound = _test.Bind(current!.Names);
        }
    }

    // The type check reads the annotation slot of the node: an untyped node carries xs:untyped (elements, documents) or xs:untypedAtomic.
    private bool MatchesType(in XPathNode node, IXdmTypeAnnotationSource annotations) =>
        _type.Value == 0 || XdmTypes.IsDerivedFrom(annotations.GetTypeAnnotation(node), _type);

    // document-node(element(N, T)): exactly one element child that matches, comments and processing instructions may accompany it.
    private bool MatchesDocumentElement(
        in XPathNode document,
        IXdmTypeAnnotationSource annotations,
        XPathNodeMatcher bound
    )
    {
        var elements = 0;
        var matches = false;
        for (var child = document.FirstChild(); !child.IsEmpty(); child = child.NextSibling())
        {
            switch (child.Kind)
            {
                case XdmNodeKind.Element:
                    elements++;
                    matches = bound.Matches(child) && MatchesType(child, annotations);
                    break;
                case XdmNodeKind.Text:
                    return false;
                default:
                    break;
            }
        }

        return elements == 1 && matches;
    }

    private enum ItemTypeKind
    {
        AnyItem = 0,
        Atomic = 1,
        AnyNode = 2,
        Node = 3,
        DocumentElement = 4,
        Function = 5,
    }
}
