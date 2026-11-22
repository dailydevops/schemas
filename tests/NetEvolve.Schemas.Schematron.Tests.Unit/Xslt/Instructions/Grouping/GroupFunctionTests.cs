namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// <c>current-group()</c> and <c>current-grouping-key()</c> (XSLT 2.0 sections 14.1 and 14.2, XSLT 3.0 section 14.2; design #150 EP-13 rows
/// G1, G2, and G5): the host functions read the slots of the evaluation context. Outside <c>xsl:for-each-group</c> a slot is unset: the
/// functions return the empty sequence under the <c>xslt</c>, <c>xslt1</c>, and <c>xslt2</c> bindings and raise the dynamic errors XTDE1061 and
/// XTDE1071 under <c>xslt3</c>. The <c>xpath</c> family does not have them.
/// </summary>
public class GroupFunctionTests
{
    private static string Call(string binding, string function, XdmSequence? group = null, XdmSequence? key = null)
    {
        NetEvolve.Arguments.ArgumentNullException.ThrowIfNull(binding);

        var functions = XsltGroupingFunctions.Create(
            new XPathQueryBinding(binding, binding.StartsWith("xslt", StringComparison.Ordinal))
        );
        var found = Array.Find(functions, f => string.Equals(f.Name.LocalName, function, StringComparison.Ordinal));
        if (found is null)
        {
            return "notfound";
        }

        using var context = new XPathEvaluationContext();
        context.Host.CurrentGroup = group;
        context.Host.CurrentGroupingKey = key;
        var call = new XPathFunctionCall(context, default);
        return found.Invoke(ref call) == XPathEvaluationStatus.Error
            ? "error:" + call.Diagnostic.Code
            : "count:" + call.Result.Count;
    }

    private static XdmSequence Items(params long[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromInteger(values[i]);
        }

        return XdmSequence.Create(items);
    }

    [Test]
    public async Task CurrentGroup_Outside_Xslt2_IsEmpty()
    {
        await Assert.That(Call("xslt2", "current-group")).IsEqualTo("count:0");

        var harness = new GroupHarness(X("value-of", 10, A("select", "concat(count(current-group()), ';')")));
        await Assert.That(harness.Result()).IsEqualTo("0;");
    }

    [Test]
    public async Task CurrentGroup_Outside_Xslt3_RaisesDynamicError()
    {
        await Assert.That(Call("xslt3", "current-group")).IsEqualTo("error:XTDE1061");

        var harness = new GroupHarness(
            X("value-of", 10, A("select", "concat(count(current-group()), ';')")),
            string.Empty,
            "xslt3"
        );
        await Assert
            .That(harness.Result())
            .IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/XTDE1061");
    }

    [Test]
    public async Task CurrentGroupingKey_Outside_PerBinding()
    {
        await Assert.That(Call("xslt2", "current-grouping-key")).IsEqualTo("count:0");
        await Assert.That(Call("xslt3", "current-grouping-key")).IsEqualTo("error:XTDE1071");

        var xslt2 = new GroupHarness(X("value-of", 10, A("select", "concat(count(current-grouping-key()), ';')")));
        var xslt3 = new GroupHarness(
            X("value-of", 10, A("select", "concat(count(current-grouping-key()), ';')")),
            string.Empty,
            "xslt3"
        );
        await Assert.That(xslt2.Result()).IsEqualTo("0;");
        await Assert
            .That(xslt3.Result())
            .IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/XTDE1071");
    }

    [Test]
    public async Task BothFunctions_Outside_Xslt2_ReturnTheEmptySequence()
    {
        var harness = new GroupHarness(
            X("value-of", 10, A("select", "concat(count(current-group()), count(current-grouping-key()), ';')"))
        );

        await Assert.That(harness.Result()).IsEqualTo("00;");
    }

    [Test]
    public async Task BothFunctions_Outside_Xslt3_RaiseTheDynamicErrors()
    {
        var group = new GroupHarness(X("value-of", 10, A("select", "count(current-group())")), string.Empty, "xslt3");
        var key = new GroupHarness(
            X("value-of", 10, A("select", "count(current-grouping-key())")),
            string.Empty,
            "xslt3"
        );

        await Assert
            .That(group.Result())
            .IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/XTDE1061");
        await Assert
            .That(key.Result())
            .IsEqualTo("problem:" + SchematronDiagnosticCodes.EvaluationFailed + "/XTDE1071");
    }

    [Test]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    public async Task BothFunctions_Outside_TheLegacyXsltNames_FollowXslt2(string binding)
    {
        await Assert.That(Call(binding, "current-group")).IsEqualTo("count:0");
        await Assert.That(Call(binding, "current-grouping-key")).IsEqualTo("count:0");
    }

    [Test]
    [Arguments("xpath")]
    [Arguments("xpath2")]
    public async Task BothFunctions_UnderAnXpathBinding_AreNotFound(string binding)
    {
        await Assert.That(Call(binding, "current-group")).IsEqualTo("notfound");
        await Assert.That(Call(binding, "current-grouping-key")).IsEqualTo("notfound");
        await Assert.That(XsltGroupingFunctions.Create(new XPathQueryBinding(binding, false)).Length).IsEqualTo(0);
    }

    [Test]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    public async Task BothFunctions_WithTheSlotsSet_ReturnThem(string binding)
    {
        await Assert.That(Call(binding, "current-group", Items(1, 2, 3), Items(9))).IsEqualTo("count:3");
        await Assert.That(Call(binding, "current-grouping-key", Items(1, 2, 3), Items(9))).IsEqualTo("count:1");
    }

    [Test]
    public async Task CurrentGroupingKey_SetToTheEmptySequence_IsEmptyUnderEveryBinding_NotTheErrorOfXslt3()
    {
        await Assert.That(Call("xslt3", "current-grouping-key", Items(1), XdmSequence.Empty)).IsEqualTo("count:0");
        await Assert.That(Call("xslt2", "current-grouping-key", Items(1), XdmSequence.Empty)).IsEqualTo("count:0");
    }

    [Test]
    public async Task BothFunctions_AreInTheDefaultFunctionNamespace_TakeNoArgument_AndNeedNoSlotFlag()
    {
        var functions = XsltGroupingFunctions.Create(new XPathQueryBinding("xslt2", true));

        await Assert.That(functions.Length).IsEqualTo(2);
        foreach (var function in functions)
        {
            await Assert.That(function.MinArity).IsEqualTo(0);
            await Assert.That(function.MaxArity).IsEqualTo(0);

            // A slot flag would hide the function from a scope whose host does not set it, and would raise XPDY0002 for an unset slot.
            await Assert.That(function.HostRequirements).IsEqualTo(XPathHostRequirements.None);
        }

        await Assert.That(functions[0].Name).IsEqualTo(XsltFunctionScope.Name("current-group"));
        await Assert.That(functions[1].Name).IsEqualTo(XsltFunctionScope.Name("current-grouping-key"));
    }

    [Test]
    public async Task Create_WithoutABinding_IsRejected() =>
        await Assert.That(() => XsltGroupingFunctions.Create(null!)).Throws<ArgumentNullException>();
}
