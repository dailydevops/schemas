namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Definitions;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.XPath.Subsets;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;

public class IdentityConstraintArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions";

    private const BindingFlags All =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    [Test]
    public async Task Folder_UsesTheXPathSubsetCheckerAndNoLinq()
    {
        var types = 0;
        var usesChecker = false;
        var linq = new List<string>();

        foreach (var type in typeof(IdentityConstraintDefinition).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
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
                    usesChecker |= declaring == typeof(XPathSubsetChecker);
                    if (string.Equals(declaring.Namespace, "System.Linq", StringComparison.Ordinal))
                    {
                        linq.Add(type.Name + "." + method.Name);
                    }
                }
            }
        }

        await Assert.That(types).IsGreaterThan(0);
        await Assert.That(usesChecker).IsTrue();
        await Assert.That(linq).IsEmpty();
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
