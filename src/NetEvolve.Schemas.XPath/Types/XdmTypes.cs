namespace NetEvolve.Schemas.XPath.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The built-in types of the data model by handle: the derivation hierarchy that <c>instance of</c>, <c>treat as</c>, and the kind
/// tests with a type name match against (XPath 2.0, 2.5.4). The XSD types come from the datatype registry of the Xml package;
/// <c>xs:anyType</c>, <c>xs:untyped</c>, and <c>xs:untypedAtomic</c> are added here because the registry holds XSD types only.
/// </summary>
/// <remarks>
/// <para>
/// Handles 1 to 15 keep the meaning of the well-known <see cref="XdmTypeId"/> values; <c>xs:anyType</c>, <c>xs:anySimpleType</c>, and
/// <c>xs:anyAtomicType</c> follow, then every other registry type in registry order. The handles are stable for the lifetime of the
/// process only.
/// </para>
/// <para>
/// Limitation: the registry of XSD 1.1 is used for every language version, so <c>xs:dateTimeStamp</c> exists in the table; the
/// binder of the sequence types hides it before XPath 3.1. User-defined and schema-imported types have no handle until schema-aware
/// processing exists (DEF-40, #52).
/// </para>
/// </remarks>
internal static class XdmTypes
{
    /// <summary>The namespace of the XML Schema built-in types.</summary>
    internal const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    // xs:anyType, xs:anySimpleType, and xs:anyAtomicType take the handles after the well-known ones.
    private const int AnyTypeHandle = 16;
    private const int AnySimpleTypeHandle = 17;
    private const int AnyAtomicTypeHandle = 18;
    private const int FirstRegistryHandle = 19;

    private static readonly string?[] Names;
    private static readonly int[] Parents;
    private static readonly bool[] Atomic;
    private static readonly Dictionary<string, XdmTypeId> ByName = new(StringComparer.Ordinal);

    static XdmTypes()
    {
        var registry = BuiltInDatatypes.For(XsdVersion.V11).All;
        var size = FirstRegistryHandle + registry.Count;
        Names = new string?[size];
        Parents = new int[size];
        Atomic = new bool[size];

        DefineDataModelTypes();
        AssignRegistryHandles(registry);
        DefineRegistryTypes(registry);
    }

    // The types of the data model that the registry of XSD types does not hold.
    private static void DefineDataModelTypes()
    {
        Define(XdmTypeId.Untyped.Value, "untyped", AnyTypeHandle, atomic: false);
        Define(XdmTypeId.UntypedAtomic.Value, "untypedAtomic", AnyAtomicTypeHandle, atomic: true);
        Define(AnyTypeHandle, "anyType", 0, atomic: false);
        Define(AnySimpleTypeHandle, "anySimpleType", AnyTypeHandle, atomic: false);
        Define(AnyAtomicTypeHandle, "anyAtomicType", AnySimpleTypeHandle, atomic: true);
    }

    // The registry types that have a well-known handle keep it; the others follow in registry order.
    private static void AssignRegistryHandles(IReadOnlyList<IDatatypeDescriptor> registry)
    {
        ByName["string"] = XdmTypeId.XsString;
        ByName["boolean"] = XdmTypeId.Boolean;
        ByName["integer"] = XdmTypeId.XsInteger;
        ByName["double"] = XdmTypeId.XsDouble;
        ByName["decimal"] = XdmTypeId.XsDecimal;
        ByName["float"] = XdmTypeId.XsFloat;
        ByName["duration"] = XdmTypeId.Duration;
        ByName["yearMonthDuration"] = XdmTypeId.YearMonthDuration;
        ByName["dayTimeDuration"] = XdmTypeId.DayTimeDuration;
        ByName["dateTime"] = XdmTypeId.DateTime;
        ByName["date"] = XdmTypeId.Date;
        ByName["time"] = XdmTypeId.Time;
        ByName["anyURI"] = XdmTypeId.AnyUri;

        var next = FirstRegistryHandle;
        for (var i = 0; i < registry.Count; i++)
        {
            var name = registry[i].Name;
            if (!ByName.ContainsKey(name))
            {
                ByName[name] = new XdmTypeId(next++);
            }
        }
    }

    // A primitive type has no base in the registry and derives from xs:anyAtomicType; a list or union from xs:anySimpleType.
    private static void DefineRegistryTypes(IReadOnlyList<IDatatypeDescriptor> registry)
    {
        for (var i = 0; i < registry.Count; i++)
        {
            var descriptor = registry[i];
            if (descriptor.Name is "anySimpleType" or "anyAtomicType")
            {
                continue;
            }

            var atomic = descriptor.Variety == DatatypeVariety.Atomic;
            var parent = atomic ? AnyAtomicTypeHandle : AnySimpleTypeHandle;
            if (atomic && descriptor.BaseType is { } baseType)
            {
                parent = ByName[baseType.Name].Value;
            }

            Define(ByName[descriptor.Name].Value, descriptor.Name, parent, atomic);
        }
    }

    private static void Define(int handle, string name, int parent, bool atomic)
    {
        Names[handle] = name;
        Parents[handle] = parent;
        Atomic[handle] = atomic;
        ByName[name] = new XdmTypeId(handle);
    }

    /// <summary>Resolves an expanded type name.</summary>
    /// <param name="namespaceUri">The namespace of the type.</param>
    /// <param name="localName">The local name of the type.</param>
    /// <param name="id">The handle, <see langword="default"/> when the name is not a built-in type.</param>
    /// <returns><see langword="true"/> when the name is a built-in type of the XML Schema namespace.</returns>
    internal static bool TryGet(string namespaceUri, string localName, out XdmTypeId id)
    {
        ArgumentNullException.ThrowIfNull(namespaceUri);
        ArgumentNullException.ThrowIfNull(localName);

        if (string.Equals(namespaceUri, SchemaNamespace, StringComparison.Ordinal))
        {
            return ByName.TryGetValue(localName, out id);
        }

        id = default;
        return false;
    }

    internal static XdmTypeId Get(string localName)
    {
        ArgumentNullException.ThrowIfNull(localName);

        return ByName.TryGetValue(localName, out var id)
            ? id
            : throw new InvalidOperationException($"'{localName}' is not a built-in type.");
    }

    internal static string? NameOf(XdmTypeId id) => id.Value > 0 && id.Value < Names.Length ? Names[id.Value] : null;

    /// <summary>Gets the type that a type derives from by restriction.</summary>
    /// <param name="id">The handle.</param>
    /// <returns>The base type; <see langword="default"/> for <c>xs:anyType</c> and for a handle outside the table.</returns>
    internal static XdmTypeId ParentOf(XdmTypeId id) =>
        id.Value > 0 && id.Value < Parents.Length ? new XdmTypeId(Parents[id.Value]) : default;

    /// <summary>Determines whether a handle names an atomic type: <c>xs:anyAtomicType</c>, <c>xs:untypedAtomic</c>, or an atomic XSD type.</summary>
    /// <param name="id">The handle.</param>
    /// <returns><see langword="true"/> for an atomic type.</returns>
    internal static bool IsAtomic(XdmTypeId id) => id.Value > 0 && id.Value < Atomic.Length && Atomic[id.Value];

    /// <summary>Determines whether a type is the other type or derives from it.</summary>
    /// <param name="type">The type.</param>
    /// <param name="baseType">The type to derive from.</param>
    /// <returns><see langword="true"/> when <paramref name="type"/> is <paramref name="baseType"/> or derives from it.</returns>
    /// <remarks>A handle outside the table is derived from itself only.</remarks>
    internal static bool IsDerivedFrom(XdmTypeId type, XdmTypeId baseType)
    {
        var target = baseType.Value;
        if (target <= 0)
        {
            return false;
        }

        var current = type.Value;
        while (current > 0 && current < Parents.Length)
        {
            if (current == target)
            {
                return true;
            }

            current = Parents[current];
        }

        return current == target;
    }
}
