namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Keys;

using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The XSLT function <c>key</c> (XSLT 2.0, 16.3) over the host slot <see cref="XPathHostSlots.Keys"/>: <c>key(name, value)</c> searches the
/// document of the context node, <c>key(name, value, top)</c> the subtree of <c>top</c>; the slot is a requirement (XPDY0002), an
/// undeclared name is XTDE1260 and a tree without a document node is XTDE1270.
/// </summary>
public class KeyFunctionTests
{
    private static string Run(IXPathKeyProvider? provider, string? contextItem, params string[] arguments) =>
        NodesHarness.RunWith(
            context =>
            {
                context.Host.Keys = provider;
                if (contextItem is not null)
                {
                    context.SetContextItem(NodesHarness.ParseItem(contextItem));
                }
            },
            "key",
            arguments
        );

    [Test]
    public async Task Key_TwoArguments_SearchesTheDocumentOfTheContextNode()
    {
        var provider = new FakeProvider();

        var result = Run(provider, "node:c", "string:k", "string:v");

        await Assert.That(result).IsEqualTo("node:a,node:b");
        await Assert.That(provider.Calls).HasSingleItem();
        await Assert.That(provider.Calls[0]).IsEqualTo("k/v/c/False");
    }

    [Test]
    public async Task Key_ThreeArguments_SearchesTheSubtreeOfTheTopNode()
    {
        var provider = new FakeProvider();

        var result = Run(provider, null, "string:k", "string:v", "node:b");

        await Assert.That(result).IsEqualTo("node:a,node:b");
        await Assert.That(provider.Calls[0]).IsEqualTo("k/v/b/True");
    }

    [Test]
    public async Task Key_ASequenceAsSecondArgument_IsHandedOverWhole()
    {
        var provider = new FakeProvider();

        _ = Run(provider, "node:c", "string:k", "string:v|integer:2|untypedAtomic:w");

        await Assert.That(provider.Calls[0]).IsEqualTo("k/v XsInteger UntypedAtomic/c/False");
    }

    [Test]
    public async Task Key_TheNodesAsSecondArgument_AreAtomized()
    {
        var provider = new FakeProvider();

        _ = Run(provider, "node:c", "string:k", "node:a@x");

        await Assert.That(provider.Calls[0]).IsEqualTo("k/UntypedAtomic/c/False");
    }

    [Test]
    public async Task Key_TheEmptySequenceOfValues_IsHandedOverEmpty()
    {
        var provider = new FakeProvider();

        _ = Run(provider, "node:c", "string:k", "()");

        await Assert.That(provider.Calls[0]).IsEqualTo("k//c/False");
    }

    [Test]
    public async Task Key_PrefixlessName_IsTheNameInNoNamespace()
    {
        var provider = new FakeProvider();

        _ = Run(provider, "node:c", "string:k", "string:v");

        await Assert.That(provider.Names[0]).IsEqualTo(new XPathQName(string.Empty, "k"));
    }

    [Test]
    public async Task Key_WithoutTheSlot_IsXPDY0002() =>
        await Assert.That(Run(null, "node:c", "string:k", "string:v")).IsEqualTo("error:XPDY0002");

    // A provider that does not know the document is not enough for the function: its contract has no document and no top node (#777).
    [Test]
    public async Task Key_ProviderWithoutTheDocumentContract_IsXPDY0002() =>
        await Assert.That(Run(new PlainProvider(), "node:c", "string:k", "string:v")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task Key_UnknownKey_IsXTDE1260() =>
        await Assert.That(Run(new FakeProvider(), "node:c", "string:missing", "string:v")).IsEqualTo("error:XTDE1260");

    [Test]
    [Arguments("1a")]
    [Arguments("a b")]
    [Arguments("a:b:c")]
    [Arguments("")]
    public async Task Key_InvalidName_IsXTDE1260(string name) =>
        await Assert.That(Run(new FakeProvider(), "node:c", "string:" + name, "string:v")).IsEqualTo("error:XTDE1260");

    // The error channel of the provider is the exception: the code of the provider is the code of the call.
    [Test]
    public async Task Key_ProviderThatThrows_PropagatesTheCode() =>
        await Assert
            .That(Run(new FakeProvider(), "node:c", "string:no-document", "string:v"))
            .IsEqualTo("error:XTDE1270");

    [Test]
    public async Task Key_TwoArguments_WithoutContextItem_IsXPDY0002() =>
        await Assert.That(Run(new FakeProvider(), null, "string:k", "string:v")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task Key_TwoArguments_WithAtomicContextItem_IsXPTY0004() =>
        await Assert.That(Run(new FakeProvider(), "integer:5", "string:k", "string:v")).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Key_ThreeArguments_WithAtomicTop_IsXPTY0004() =>
        await Assert
            .That(Run(new FakeProvider(), null, "string:k", "string:v", "integer:5"))
            .IsEqualTo("error:XPTY0004");

    [Test]
    [Arguments(1, XPathVersion.V20, XPathHostRequirements.Xslt20Host, false)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(3, XPathVersion.V20, XPathHostRequirements.Xslt20Host, true)]
    [Arguments(4, XPathVersion.V20, XPathHostRequirements.Xslt20Host, false)]
    [Arguments(2, XPathVersion.V20, XPathHostRequirements.None, false)]
    [Arguments(2, XPathVersion.V31, XPathHostRequirements.None, false)]
    public async Task Key_IsFoundOnlyInTheXslt20Host(
        int arity,
        XPathVersion version,
        XPathHostRequirements host,
        bool expected
    ) => await Assert.That(NodesHarness.TryResolve("key", arity, out _, version, host)).IsEqualTo(expected);

    // Over real XPath: the call is compiled and evaluated like any other expression, with the context node as the focus.
    [Test]
    [Arguments("key('k', 'v')", "node:a,node:b")]
    [Arguments("key('k', ('v', 'w'))", "node:a,node:b")]
    [Arguments("key('k', 'v', /r/b)", "node:a,node:b")]
    public async Task Key_InAnExpression_IsEvaluated(string text, string expected)
    {
        var compiled = XPathExpression.TryCompile(
            text,
            XPathStaticContext.Default,
            XPathHostRequirements.Xslt20Host | XPathHostRequirements.Keys
        );
        using var context = new XPathEvaluationContext();
        context.Host.Keys = new FakeProvider();
        context.SetContextItem(NodesHarness.ParseItem("node:c"));

        await Assert.That(compiled.Success).IsTrue().Because(compiled.Diagnostic.Message);
        var result = compiled.Expression!.TryEvaluate(context);
        await Assert.That(result.Status).IsEqualTo(XPathEvaluationStatus.Ok).Because(result.Diagnostic.Message);
        await Assert.That(NodesHarness.Show(result.Value)).IsEqualTo(expected);
    }

    // The provider of the tests: the key 'k' finds a and b; 'no-document' is a tree without a document node; every other name is unknown.
    private sealed class FakeProvider : IXPathDocumentKeyProvider
    {
        internal List<string> Calls { get; } = [];

        internal List<XPathQName> Names { get; } = [];

        public bool TryGetNodes(XPathQName name, in XdmItem value, out XdmSequence nodes)
        {
            nodes = XdmSequence.Empty;
            return false;
        }

        public bool TryLookup(
            in XPathQName name,
            in XdmSequence values,
            in XPathNode node,
            bool subtree,
            out XdmSequence nodes
        )
        {
            nodes = XdmSequence.Empty;
            if (name.LocalName == "no-document")
            {
                throw new XPathException(
                    new XPathDiagnostic(
                        new XPathErrorCode("XTDE1270"),
                        XPathErrorKind.Dynamic,
                        "The tree has no document node.",
                        -1,
                        0
                    )
                );
            }

            if (name.LocalName != "k")
            {
                return false;
            }

            var texts = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                _ = values[i].TryGetString(out var text);
                texts.Add(text ?? values[i].Kind.ToString());
            }

            Names.Add(name);
            Calls.Add($"{name.LocalName}/{string.Join(' ', texts)}/{Key(node)}/{subtree}");
            nodes = NodesHarness.Parse("node:a|node:b");
            return true;
        }

        private static string Key(XPathNode node)
        {
            foreach (var entry in NodesHarness.Nodes)
            {
                if (entry.Value.IsSameNode(in node))
                {
                    return entry.Key;
                }
            }

            return "unknown";
        }
    }

    private sealed class PlainProvider : IXPathKeyProvider
    {
        public bool TryGetNodes(XPathQName name, in XdmItem value, out XdmSequence nodes)
        {
            nodes = XdmSequence.Empty;
            return false;
        }
    }
}
