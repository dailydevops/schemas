namespace NetEvolve.Schemas.Schematron.Tests.Unit.Svrl;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Xml.Writer;

public class SvrlArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Svrl";

    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    [Test]
    public async Task Svrl_ReferencesOnlyTheXmlWriterContract_NoSystemXml()
    {
        var assembly = typeof(SvrlReportSink).Assembly;
        var violations = new List<string>();
        var checkedTypes = 0;
        var usesWriter = false;

        foreach (var type in assembly.GetTypes())
        {
            if (!(type.Namespace?.StartsWith(Folder, StringComparison.Ordinal) ?? false))
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
                usesWriter |= Scan(method, type, violations);
            }

            foreach (var ctor in type.GetConstructors(All))
            {
                usesWriter |= Scan(ctor, type, violations);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(0);
        await Assert.That(usesWriter).IsTrue();
        await Assert.That(violations).IsEmpty();
    }

    private static void Collect(Type used, Type owner, List<string> violations)
    {
        if (used.Namespace?.StartsWith("System.Xml", StringComparison.Ordinal) ?? false)
        {
            violations.Add($"{owner.Name} references {used.FullName}");
        }
    }

    // Scans the IL for newobj/call/callvirt tokens and the local variable types; the result tells whether the
    // writer is constructed. A token that does not resolve is no operand and is skipped.
    private static bool Scan(MethodBase method, Type owner, List<string> violations)
    {
        var body = method.GetMethodBody();
        var il = body?.GetILAsByteArray();
        if (body is null || il is null)
        {
            return false;
        }

        foreach (var local in body.LocalVariables)
        {
            Collect(local.LocalType, owner, violations);
        }

        var writer = false;
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

            writer |= declaring == typeof(XmlWriter) && il[i] == 0x73;
            Collect(declaring, owner, violations);
        }

        return writer;
    }
}
