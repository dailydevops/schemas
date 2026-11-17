namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Construction;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Model;

public class ConstructionArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Construction";

    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    [Test]
    public async Task Construction_CreatesNodesOnlyThroughXmlTreeBuilder()
    {
        var assembly = typeof(ItemSequenceSink).Assembly;
        var violations = new List<string>();
        var checkedTypes = 0;
        var usesBuilder = false;

        foreach (var type in assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                continue;
            }

            checkedTypes++;
            foreach (var field in type.GetFields(All))
            {
                Collect(field.FieldType, type, violations);
            }

            foreach (var method in type.GetMethods(All))
            {
                usesBuilder |= Scan(method, type, violations);
            }

            foreach (var ctor in type.GetConstructors(All))
            {
                usesBuilder |= Scan(ctor, type, violations);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(0);
        await Assert.That(usesBuilder).IsTrue();
        await Assert.That(violations).IsEmpty();
    }

    private static void Collect(Type used, Type owner, List<string> violations)
    {
        if (
            used == typeof(XmlDocumentTree)
            || (used.Namespace?.StartsWith("System.Xml", StringComparison.Ordinal) ?? false)
        )
        {
            violations.Add($"{owner.Name} references {used.FullName}");
        }
    }

    // Scans the IL for newobj/call tokens; a token that does not resolve is no operand and is skipped.
    private static bool Scan(MethodBase method, Type owner, List<string> violations)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        var builder = false;
        if (il is null)
        {
            return false;
        }

        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] is not (0x73 or 0x28 or 0x6F))
            {
                continue;
            }

            MethodBase? target;
            try
            {
                target = method.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1));
            }
            catch (Exception ex) when (ex is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            var declaring = target?.DeclaringType;
            if (declaring is null)
            {
                continue;
            }

            builder |= declaring == typeof(XmlTreeBuilder) && il[i] == 0x73;
            Collect(declaring, owner, violations);
        }

        return builder;
    }
}
