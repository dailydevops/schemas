namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

public class EvaluationArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation";

    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    // Idc_Streaming_NoTree: the evaluation owns no tree and uses neither the BCL XML nor the BCL JSON types, nor LINQ.
    [Test]
    public async Task Folder_ReferencesNoTreeBclXmlJsonOrLinqType()
    {
        var types = 0;
        var forbidden = new List<string>();

        foreach (var type in typeof(IdentityConstraintListener).Assembly.GetTypes())
        {
            if (!(type.Namespace ?? string.Empty).StartsWith(Folder, StringComparison.Ordinal))
            {
                continue;
            }

            types++;
            var methods = new List<MethodBase>(type.GetMethods(All));
            methods.AddRange(type.GetConstructors(All));
            foreach (var method in methods)
            {
                foreach (var declaring in CalledTypes(method))
                {
                    var ns = declaring.Namespace ?? string.Empty;
                    if (
                        ns.StartsWith("System.Xml", StringComparison.Ordinal)
                        || ns.StartsWith("System.Linq", StringComparison.Ordinal)
                        || ns.StartsWith("System.Text.Json", StringComparison.Ordinal)
                    )
                    {
                        forbidden.Add(type.Name + "." + method.Name + " -> " + declaring.FullName);
                    }
                }
            }
        }

        await Assert.That(types).IsGreaterThan(0);
        await Assert.That(forbidden).IsEmpty();
    }

    // Scans the IL for call, newobj, callvirt, and field tokens; a token that does not resolve is no operand.
    private static List<Type> CalledTypes(MethodBase method)
    {
        var result = new List<Type>();
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return result;
        }

        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] is not (0x73 or 0x28 or 0x6F or 0x7B or 0x7E))
            {
                continue;
            }

            try
            {
                var target = method.Module.ResolveMember(BitConverter.ToInt32(il, i + 1));
                if (target?.DeclaringType is { } declaring)
                {
                    result.Add(declaring);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or BadImageFormatException)
            {
                // Not an operand token.
            }
        }

        return result;
    }
}
