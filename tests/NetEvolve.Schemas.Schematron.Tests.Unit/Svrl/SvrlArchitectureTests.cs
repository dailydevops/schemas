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
                CollectSignature(method, type, violations);
                usesWriter |= Scan(method, type, violations);
            }

            foreach (var ctor in type.GetConstructors(All))
            {
                CollectSignature(ctor, type, violations);
                usesWriter |= Scan(ctor, type, violations);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(0);
        await Assert.That(usesWriter).IsTrue();
        await Assert.That(violations).IsEmpty();
    }

    // The writer contract: the writer itself, the sink contract types, and the name table the writer resolves symbols with.
    private static readonly string[] AllowedXml =
    [
        "NetEvolve.Schemas.Xml.Writer",
        "NetEvolve.Schemas.Xml.Contracts",
        "NetEvolve.Schemas.Xml.Text.NameTable",
    ];

    private static void Collect(Type used, Type owner, List<string> violations)
    {
        var ns = used.Namespace ?? string.Empty;
        var isSystemXml = ns.StartsWith("System.Xml", StringComparison.Ordinal);
        var isOtherXml =
            ns.StartsWith("NetEvolve.Schemas.Xml", StringComparison.Ordinal) && Array.IndexOf(AllowedXml, ns) < 0;
        if (isSystemXml || isOtherXml)
        {
            violations.Add($"{owner.Name} references {used.FullName}");
        }
    }

    private static void CollectSignature(MethodBase method, Type owner, List<string> violations)
    {
        foreach (var parameter in method.GetParameters())
        {
            Collect(parameter.ParameterType, owner, violations);
        }

        if (method is MethodInfo info)
        {
            Collect(info.ReturnType, owner, violations);
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
