namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Interop;

public class SystemXmlBoundaryTests
{
    private const BindingFlags AllMembers =
        BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.DeclaredOnly;

    private static bool IsSystemXml(Type type) =>
        type.Namespace is { } ns
        && (
            string.Equals(ns, "System.Xml", StringComparison.Ordinal)
            || ns.StartsWith("System.Xml.", StringComparison.Ordinal)
        );

    private static bool IsInterop(Type type) =>
        type.Namespace is { } ns
        && (ns.EndsWith(".Interop", StringComparison.Ordinal) || ns.Contains(".Interop.", StringComparison.Ordinal));

    private static void Collect(Type type, HashSet<Type> into)
    {
        if (type.HasElementType)
        {
            Collect(type.GetElementType()!, into);
            return;
        }

        if (!into.Add(type))
        {
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                Collect(argument, into);
            }
        }
    }

    // Every type that appears in a signature, field, base type, or interface of the given type.
    private static HashSet<Type> Referenced(Type type)
    {
        var found = new HashSet<Type>();
        if (type.BaseType is { } baseType)
        {
            Collect(baseType, found);
        }

        foreach (var contract in type.GetInterfaces())
        {
            Collect(contract, found);
        }

        foreach (var field in type.GetFields(AllMembers))
        {
            Collect(field.FieldType, found);
        }

        foreach (var property in type.GetProperties(AllMembers))
        {
            Collect(property.PropertyType, found);
        }

        foreach (var method in type.GetMethods(AllMembers))
        {
            Collect(method.ReturnType, found);
            foreach (var parameter in method.GetParameters())
            {
                Collect(parameter.ParameterType, found);
            }
        }

        foreach (var constructor in type.GetConstructors(AllMembers))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                Collect(parameter.ParameterType, found);
            }
        }

        return found;
    }

    private static List<string> TypesReferencingSystemXml(Func<Type, bool> include)
    {
        var result = new List<string>();
        foreach (var type in typeof(XmlTree).Assembly.GetTypes())
        {
            if (!include(type))
            {
                continue;
            }

            foreach (var referenced in Referenced(type))
            {
                if (IsSystemXml(referenced))
                {
                    result.Add(type.FullName + " -> " + referenced.FullName);
                }
            }
        }

        return result;
    }

    [Test]
    public async Task ContractTypes_ReferenceNoSystemXmlType() =>
        await Assert
            .That(
                TypesReferencingSystemXml(type =>
                    type.Namespace?.StartsWith("NetEvolve.Schemas.Xml.Contracts", StringComparison.Ordinal) == true
                )
            )
            .IsEmpty();

    [Test]
    public async Task InteropTypes_ReferenceSystemXml_OnlyInInteropNamespace()
    {
        var outside = TypesReferencingSystemXml(type => !IsInterop(type));
        var inside = TypesReferencingSystemXml(IsInterop);

        await Assert.That(outside).IsEmpty();
        await Assert.That(inside).IsNotEmpty();
    }

    [Test]
    public async Task InteropNamespace_HoldsExactlyTheApprovedPublicTypes()
    {
        var names = new List<string>();
        foreach (var type in typeof(XmlTree).Assembly.GetExportedTypes())
        {
            if (IsInterop(type))
            {
                names.Add(type.Name);
            }
        }

        await Assert.That(names).IsEquivalentTo([nameof(XmlInterop), nameof(XmlInteropOptions)]);
    }

    [Test]
    public async Task Library_HasNoTypeNamedXmlDocument_SoSystemXmlXmlDocumentStaysUnambiguous()
    {
        var clashes = new List<string>();
        foreach (var type in typeof(XmlTree).Assembly.GetExportedTypes())
        {
            if (string.Equals(type.Name, "XmlDocument", StringComparison.Ordinal))
            {
                clashes.Add(type.FullName!);
            }
        }

        await Assert.That(clashes).IsEmpty();
    }
}
