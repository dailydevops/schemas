namespace NetEvolve.Schemas.Xsd.Components;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The built-in types of a version: <c>anyType</c> plus every datatype of the Xml package registry. They are created once
/// per version and shared by every registry, because they are immutable.
/// </summary>
internal static class XsdBuiltInTypes
{
    private static readonly Lazy<IReadOnlyList<IXsdComponent>> V10 = new(static () => Create(XsdVersion.V10));
    private static readonly Lazy<IReadOnlyList<IXsdComponent>> V11 = new(static () => Create(XsdVersion.V11));

    /// <summary>Gets the built-in types of a version; the same list on every call.</summary>
    /// <param name="version">The XSD version.</param>
    /// <returns>The types, base types before the types derived from them.</returns>
    internal static IReadOnlyList<IXsdComponent> For(XsdVersion version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        return version == XsdVersion.V10 ? V10.Value : V11.Value;
    }

    /// <summary>Registers the built-in types of a version.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="version">The XSD version.</param>
    internal static void Register(IComponentRegistry registry, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var types = For(version);
        for (var i = 0; i < types.Count; i++)
        {
            registry.Add(types[i]);
        }
    }

    private static List<IXsdComponent> Create(XsdVersion version)
    {
        var registry = BuiltInDatatypes.For(version);
        var anyType = new XsdBuiltInType(XsdComponentKind.ComplexType, "anyType", null, null);
        var anySimpleDescriptor = registry.Get("anySimpleType");
        var anySimpleType = new XsdBuiltInType(
            XsdComponentKind.SimpleType,
            "anySimpleType",
            anySimpleDescriptor,
            anyType
        );
        var byName = new Dictionary<string, XsdBuiltInType>(StringComparer.Ordinal)
        {
            ["anySimpleType"] = anySimpleType,
        };
        var all = new List<IXsdComponent> { anyType, anySimpleType };
        foreach (var descriptor in registry.All)
        {
            // The datatype contract leaves the base of a primitive type empty; every simple type derives from anySimpleType.
            var baseType =
                descriptor.BaseType is not null && byName.TryGetValue(descriptor.BaseType.Name, out var found)
                    ? found
                    : anySimpleType;
            if (byName.ContainsKey(descriptor.Name))
            {
                continue;
            }

            var type = new XsdBuiltInType(XsdComponentKind.SimpleType, descriptor.Name, descriptor, baseType);
            byName.Add(descriptor.Name, type);
            all.Add(type);
        }

        return all;
    }
}
