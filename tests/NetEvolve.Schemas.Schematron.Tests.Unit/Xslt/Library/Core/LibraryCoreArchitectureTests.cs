namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>The Library/Core folder holds three functions and the scope; the implementation of <c>generate-id</c> stays in the XPath package.</summary>
public class LibraryCoreArchitectureTests
{
    private const string Folder = "NetEvolve.Schemas.Schematron.Xslt.Library.Core";

    [Test]
    public async Task LibraryCore_ContainsNoGenerateIdImplementation()
    {
        var checkedTypes = 0;
        var violations = new List<string>();
        foreach (var type in typeof(XsltFunctionScope).Assembly.GetTypes())
        {
            if (!string.Equals(type.Namespace, Folder, StringComparison.Ordinal))
            {
                continue;
            }

            checkedTypes++;
            if (type.Name.Contains("GenerateId", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(type.FullName!);
            }
        }

        await Assert.That(checkedTypes).IsGreaterThan(0);
        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task LibraryCore_RegistersExactlyCurrentSystemPropertyAndFunctionAvailable()
    {
        var names = new List<string>();
        foreach (var function in Scope().CoreFunctions)
        {
            names.Add($"{function.Name.LocalName}/{function.MinArity}-{function.MaxArity}");
        }

        names.Sort(StringComparer.Ordinal);

        await Assert.That(string.Join(",", names)).IsEqualTo("current/0-0,function-available/1-2,system-property/1-1");
    }

    [Test]
    public async Task LibraryCore_FunctionsAreInTheDefaultFunctionNamespaceAndNeedOnlyTheCurrentItemSlot()
    {
        foreach (var function in Scope().CoreFunctions)
        {
            await Assert.That(function.Name.NamespaceName).IsEqualTo(FunctionNamespace);
            await Assert
                .That(function.HostRequirements is XPathHostRequirements.None or XPathHostRequirements.CurrentItem)
                .IsTrue();
        }
    }
}
