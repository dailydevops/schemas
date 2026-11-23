namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>generate-id</c> (XSLT 2.0, 16.6; Functions and Operators 3.0, 14.6): one implementation for the XSLT 2.0 host and for XPath 3.0
/// and later, stable and distinct per node, empty for the empty sequence, and not found in plain XPath 2.0.
/// </summary>
public class GenerateIdTests
{
    private const string Prefix = "string:";

    private static string Id(string node) => NodesHarness.Run("generate-id", "node:" + node)[Prefix.Length..];

#if NET7_0_OR_GREATER
    private static bool IsAsciiLetter(char c) => char.IsAsciiLetter(c);

    private static bool IsAsciiLetterOrDigit(char c) => char.IsAsciiLetterOrDigit(c);
#else
    private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    private static bool IsAsciiLetterOrDigit(char c) => IsAsciiLetter(c) || (c >= '0' && c <= '9');
#endif

    private static bool IsNcName(string text)
    {
        if (text.Length == 0 || !IsAsciiLetter(text[0]))
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    [Test]
    [Arguments("doc")]
    [Arguments("a")]
    [Arguments("a@x")]
    [Arguments("a/t")]
    [Arguments("cm")]
    [Arguments("pi")]
    public async Task GenerateId_ForTheSameNode_ReturnsTheSameId(string node)
    {
        var first = NodesHarness.Run("generate-id", "node:" + node);
        var second = NodesHarness.Run("generate-id", "node:" + node);
        var viaContext = NodesHarness.RunWithContextItem("node:" + node, "generate-id");

        await Assert.That(first).StartsWith(Prefix);
        await Assert.That(second).IsEqualTo(first);
        await Assert.That(viaContext).IsEqualTo(first);
    }

    [Test]
    public async Task GenerateId_ForTheSameNodeInTwoEvaluations_ReturnsTheSameId()
    {
        using var one = new XPathEvaluationContext();
        using var two = new XPathEvaluationContext();

        var first = NodesHarness.RunIn(one, "generate-id", "node:a");
        var second = NodesHarness.RunIn(two, "generate-id", "node:a");

        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task GenerateId_ForEveryNodeOfTwoTrees_ReturnsADistinctId()
    {
        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        foreach (var key in NodesHarness.Nodes.Keys)
        {
            await Assert.That(seen.Add(Id(key))).IsTrue().Because("the id of " + key + " repeats an earlier one");
        }

        await Assert.That(seen.Count).IsEqualTo(NodesHarness.Nodes.Count);
    }

    [Test]
    public async Task GenerateId_ForEveryNode_IsAValidNCName()
    {
        foreach (var key in NodesHarness.Nodes.Keys)
        {
            var id = Id(key);

            await Assert.That(IsNcName(id)).IsTrue().Because("'" + id + "' of " + key + " is not an NCName");
        }
    }

    [Test]
    public async Task GenerateId_ForTwoWrappersOfTheSameNode_ReturnsTheSameId()
    {
        var node = NodesHarness.Nodes["c"];
        var again = new XPathNode(node.Node);
        using var context = new XPathEvaluationContext();
        _ = FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName(NodesHarness.FunctionNamespace, "generate-id"),
            1,
            XPathHostRequirements.Xslt20Host,
            out var function
        );

        var first = function!.Call(context, [XdmSequence.Create(XdmItem.FromNode(node))]);
        var second = function.Call(context, [XdmSequence.Create(XdmItem.FromNode(again))]);

        await Assert.That(NodesHarness.Show(second)).IsEqualTo(NodesHarness.Show(first));
    }

    [Test]
    public async Task GenerateId_ForTheEmptySequence_IsTheEmptyString()
    {
        await Assert.That(NodesHarness.Run("generate-id", "()")).IsEqualTo("string:");
        await Assert.That(NodesHarness.RunWithContextItem("node:a", "generate-id", "()")).IsEqualTo("string:");
    }

    [Test]
    public async Task GenerateId_WithoutArgumentAndWithoutContextItem_RaisesXPDY0002() =>
        await Assert.That(NodesHarness.Run("generate-id")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task GenerateId_WithNonNodeContextItem_RaisesXPTY0004() =>
        await Assert.That(NodesHarness.RunWithContextItem("integer:5", "generate-id")).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments("integer:5")]
    [Arguments("node:a|node:b")]
    public async Task GenerateId_WithNonNodeOrSeveralItems_RaisesXPTY0004(string argument) =>
        await Assert.That(NodesHarness.Run("generate-id", argument)).IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments(0, XPathVersion.V20, XPathHostRequirements.None, false)]
    [Arguments(1, XPathVersion.V20, XPathHostRequirements.None, false)]
    [Arguments(0, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(1, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(0, XPathVersion.V30, XPathHostRequirements.None, true)]
    [Arguments(1, XPathVersion.V30, XPathHostRequirements.None, true)]
    [Arguments(1, XPathVersion.V30, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(0, XPathVersion.V31, XPathHostRequirements.None, true)]
    [Arguments(1, XPathVersion.V31, XPathHostRequirements.None, true)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.Xslt20Host, false)]
    [Arguments(2, XPathVersion.V30, XPathHostRequirements.None, false)]
    [Arguments(1, XPathVersion.V20, XPathHostRequirements.CurrentItem, false)]
    public async Task GenerateId_IsFoundOnlyForTheXslt20HostAndForXPath30AndLater(
        int arity,
        XPathVersion version,
        XPathHostRequirements host,
        bool expected
    )
    {
        var found = NodesHarness.TryResolve("generate-id", arity, out var function, version, host);

        await Assert.That(found).IsEqualTo(expected);
        if (found)
        {
            await Assert.That(function!.Name).IsEqualTo(new XPathQName(NodesHarness.FunctionNamespace, "generate-id"));
        }
    }

    [Test]
    [Arguments(XPathVersion.V20, true)]
    [Arguments(XPathVersion.V30, true)]
    [Arguments(XPathVersion.V31, true)]
    public async Task GenerateId_IsDefinedByTheCatalog_SoNoHostLayerNeedsItsOwnCopy(
        XPathVersion version,
        bool expected
    ) =>
        await Assert
            .That(
                FunctionCatalog.Default.Registry.IsDefinedByCatalog(
                    version,
                    new XPathQName(NodesHarness.FunctionNamespace, "generate-id"),
                    1
                )
            )
            .IsEqualTo(expected);
}
