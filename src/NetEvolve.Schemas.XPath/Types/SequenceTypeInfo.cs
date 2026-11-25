namespace NetEvolve.Schemas.XPath.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A sequence type as the signature of a function item holds it: enough to decide whether one sequence type is a subtype of another
/// (XPath 3.0, 2.5.5.2), and the conversion that applies to a value of the type (XPath 3.0, 3.1.5.2, function conversion rules).
/// Immutable.
/// </summary>
/// <remarks>
/// Limitations: a kind test is compared by its kind and its names as written, so two element tests that name the same expanded name through
/// different prefixes are not subtypes of each other; the numeric pseudo type of a built-in parameter is the union of the three primitive numeric types.
/// </remarks>
internal sealed class SequenceTypeInfo
{
    private static readonly XdmTypeId AnyAtomic = XdmTypes.Get("anyAtomicType");

    private readonly ItemKind _kind;
    private readonly XdmTypeId _atomic;
    private readonly string? _node;
    private readonly bool _emptySequence;
    private readonly SequenceTypeInfo? _mapValue;

    private SequenceTypeInfo(
        ItemKind kind,
        XdmTypeId atomic,
        string? node,
        FunctionSignature? function,
        Occurrence occurrence,
        bool emptySequence,
        XPathParameterType conversion,
        SequenceTypeInfo? mapValue = null
    )
    {
        _mapValue = mapValue;
        _kind = kind;
        _atomic = atomic;
        _node = node;
        Function = function;
        Occurrence = occurrence;
        _emptySequence = emptySequence;
        Conversion = conversion;
    }

    /// <summary>Gets <c>item()*</c>: the type of a parameter or result that declares no type.</summary>
    public static SequenceTypeInfo AnyItems { get; } =
        new(
            ItemKind.Any,
            default,
            null,
            null,
            Occurrence.ZeroOrMore,
            emptySequence: false,
            new XPathParameterType(XPathItemType.Item, Occurrence.ZeroOrMore)
        );

    /// <summary>Gets the signature of a typed function test, <see langword="null"/> for <c>function(*)</c> and for a type that is not a function test.</summary>
    public FunctionSignature? Function { get; }

    /// <summary>Gets the occurrence indicator.</summary>
    public Occurrence Occurrence { get; }

    /// <summary>Gets the conversion that applies to a value of the type.</summary>
    public XPathParameterType Conversion { get; }

    /// <summary>Binds a sequence type.</summary>
    /// <param name="syntax">The syntax of the sequence type.</param>
    /// <param name="context">The static context that resolves prefixes.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="syntax"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="NetEvolve.Schemas.XPath.Errors.XPathException">A prefix is not bound (XPST0081) or a type name is not a built-in type (XPST0051).</exception>
    public static SequenceTypeInfo Bind(SequenceType syntax, XPathStaticContext context)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentNullException.ThrowIfNull(context);

        // The conversion binds, and so validates, the item type first.
        var conversion = XPathParameterType.FromSequenceType(syntax, context);
        if (syntax.ItemType is null)
        {
            return new(ItemKind.Any, default, null, null, Occurrence.ZeroOrOne, emptySequence: true, conversion);
        }

        var item = syntax.ItemType;
        while (item is ParenthesizedItemType parenthesized)
        {
            item = parenthesized.Inner;
        }

        switch (item)
        {
            case AtomicType atomic:
                var type = ItemTypeMatcher.Bind(atomic, context).Type;
                return new(ItemKind.Atomic, type, null, null, syntax.Occurrence, false, conversion);
            case KindItemType kind:
                return new(ItemKind.Node, default, NodeKey(kind.Test), null, syntax.Occurrence, false, conversion);
            case AnyFunctionTest:
                return new(ItemKind.Function, default, null, null, syntax.Occurrence, false, conversion);
            case TypedFunctionTest typed:
                var parameters = new SequenceTypeInfo[typed.ParameterTypes.Count];
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = Bind(typed.ParameterTypes[i], context);
                }

                var signature = new FunctionSignature(parameters, Bind(typed.ReturnType, context));
                return new(ItemKind.Function, default, null, signature, syntax.Occurrence, false, conversion);
            case AnyMapTest:
                return new(ItemKind.Map, default, null, null, syntax.Occurrence, false, conversion);
            case TypedMapTest map:
                var key = ItemTypeMatcher.Bind(map.KeyType, context).Type;
                return new(
                    ItemKind.Map,
                    key,
                    null,
                    null,
                    syntax.Occurrence,
                    false,
                    conversion,
                    Bind(map.ValueType, context)
                );
            default:
                return new(ItemKind.Any, default, null, null, syntax.Occurrence, false, conversion);
        }
    }

    /// <summary>Describes the declared type of a parameter of a built-in function.</summary>
    /// <param name="parameter">The declared type.</param>
    /// <returns>The type; a parameter that a kind test constrains is described as <c>item()*</c>.</returns>
    public static SequenceTypeInfo FromParameter(XPathParameterType parameter)
    {
        if (parameter.Matcher is not null)
        {
            return new(ItemKind.Any, default, null, null, Occurrence.ZeroOrMore, false, parameter);
        }

        var occurrence = parameter.Occurrence;
        return parameter.ItemType switch
        {
            XPathItemType.Item => new(ItemKind.Any, default, null, null, occurrence, false, parameter),
            XPathItemType.Node => new(ItemKind.Node, default, null, null, occurrence, false, parameter),
            XPathItemType.AnyAtomic => new(ItemKind.Atomic, AnyAtomic, null, null, occurrence, false, parameter),
            XPathItemType.Numeric => new(ItemKind.Numeric, default, null, null, occurrence, false, parameter),
            _ when parameter.AtomicTypeId.Value > 0 => new(
                ItemKind.Atomic,
                parameter.AtomicTypeId,
                null,
                null,
                occurrence,
                false,
                parameter
            ),
            _ => new(ItemKind.Any, default, null, null, occurrence, false, parameter),
        };
    }

    /// <summary>Determines whether every value of this type is also a value of another type.</summary>
    /// <param name="other">The other type.</param>
    /// <returns><see langword="true"/> when this type is a subtype of <paramref name="other"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool IsSubtypeOf(SequenceTypeInfo other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (_emptySequence)
        {
            return other._emptySequence || other.Occurrence is Occurrence.ZeroOrOne or Occurrence.ZeroOrMore;
        }

        return !other._emptySequence && OccurrenceIsSubtype(Occurrence, other.Occurrence) && ItemIsSubtype(other);
    }

    private static bool OccurrenceIsSubtype(Occurrence sub, Occurrence super) =>
        super == Occurrence.ZeroOrMore
        || sub == super
        || (super == Occurrence.ZeroOrOne && sub == Occurrence.ExactlyOne)
        || (super == Occurrence.OneOrMore && sub == Occurrence.ExactlyOne);

    private bool ItemIsSubtype(SequenceTypeInfo other)
    {
        if (other._kind == ItemKind.Any)
        {
            return true;
        }

        switch (_kind)
        {
            case ItemKind.Atomic:
                return other._kind switch
                {
                    ItemKind.Atomic => XdmTypes.IsDerivedFrom(_atomic, other._atomic),
                    ItemKind.Numeric => IsNumeric(_atomic),
                    _ => false,
                };
            case ItemKind.Numeric:
                return other._kind == ItemKind.Numeric
                    || (other._kind == ItemKind.Atomic && other._atomic == AnyAtomic);
            case ItemKind.Node:
                return other._kind == ItemKind.Node && NodeIsSubtype(_node, other._node);
            case ItemKind.Function:
                return other._kind == ItemKind.Function
                    && (other.Function is null || (Function is not null && Function.IsSubtypeOf(other.Function)));
            case ItemKind.Map:
                return other._kind switch
                {
                    ItemKind.Map => other._mapValue is null
                        || (
                            _mapValue is not null
                            && XdmTypes.IsDerivedFrom(_atomic, other._atomic)
                            && _mapValue.IsSubtypeOf(other._mapValue)
                        ),
                    ItemKind.Function => other.Function is null,
                    _ => false,
                };
            default:
                return false;
        }
    }

    private static bool IsNumeric(XdmTypeId type) =>
        XdmTypes.IsDerivedFrom(type, XdmTypeId.XsDecimal)
        || XdmTypes.IsDerivedFrom(type, XdmTypeId.XsFloat)
        || XdmTypes.IsDerivedFrom(type, XdmTypeId.XsDouble);

    // A null key is node(); otherwise the key names the kind test with its names, and a test without names covers every test of its kind.
    private static bool NodeIsSubtype(string? sub, string? super)
    {
        if (super is null)
        {
            return true;
        }

        if (sub is null)
        {
            return false;
        }

        if (string.Equals(sub, super, StringComparison.Ordinal))
        {
            return true;
        }

        var general =
            super.EndsWith("()", StringComparison.Ordinal) || super.EndsWith("(*,*)", StringComparison.Ordinal);
        return general && sub.StartsWith(super.Split('(')[0] + "(", StringComparison.Ordinal);
    }

    private static string? NodeKey(KindTest test) =>
        test switch
        {
            AnyKindTest => null,
            ElementTest element => "element(" + Name(element.Name) + "," + Name(element.TypeName) + ")",
            AttributeTest attribute => "attribute(" + Name(attribute.Name) + "," + Name(attribute.TypeName) + ")",
            DocumentTest { Element: { } element } => "document-node(" + NodeKey(element) + ")",
            DocumentTest => "document-node()",
            ProcessingInstructionTest instruction => "processing-instruction(" + instruction.Target + ")",
            _ => test.GetType().Name + "()",
        };

    private static string Name(QualifiedName? name) =>
        name is null ? "*" : (name.Prefix is null ? string.Empty : name.Prefix + ":") + name.LocalName;

    private enum ItemKind
    {
        Any = 0,
        Atomic = 1,
        Numeric = 2,
        Node = 3,
        Function = 4,
        Map = 5,
    }
}
