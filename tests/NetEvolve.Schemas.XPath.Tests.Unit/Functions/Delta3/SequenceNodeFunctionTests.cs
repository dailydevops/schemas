namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Delta3;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The sequence and node functions of XPath 3.0 and 3.1 (Functions and Operators 3.1, 14.1, 14.2, 14.4, and 14.5): <c>fn:head</c>,
/// <c>fn:tail</c>, <c>fn:innermost</c>, <c>fn:outermost</c>, <c>fn:path</c>, and <c>fn:has-children</c>.
/// </summary>
public class SequenceNodeFunctionTests
{
    [Test]
    [Arguments("head", 1)]
    [Arguments("tail", 1)]
    [Arguments("innermost", 1)]
    [Arguments("outermost", 1)]
    [Arguments("path", 0)]
    [Arguments("path", 1)]
    [Arguments("has-children", 0)]
    [Arguments("has-children", 1)]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(
        string local,
        int arity,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V20)).IsFalse();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V30)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve(local, arity, out _, XPathVersion.V31)).IsTrue();
    }

    [Test]
    [Arguments("fn:head((1))")]
    [Arguments("fn:tail((1))")]
    [Arguments("fn:innermost(.)")]
    [Arguments("fn:outermost(.)")]
    [Arguments("fn:path(.)")]
    [Arguments("fn:path()")]
    [Arguments("fn:has-children(.)")]
    [Arguments("fn:has-children()")]
    public async Task TryCompile_InXPath20_ReturnsXpst0017(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V20)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .Build();

        await Assert.That(XPathExpression.TryCompile(query, statics).Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    [Arguments("1 2 3 4 5", "1")]
    [Arguments("9", "9")]
    [Arguments("", "")]
    public async Task Head_ReturnsTheFirstItemOrTheEmptySequence(
        string values,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(values);

        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("head", Delta3Harness.IntsOf(values))))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments("1 2 3 4 5", "2|3|4|5")]
    [Arguments("1 2", "2")]
    [Arguments("9", "")]
    [Arguments("", "")]
    public async Task Tail_ReturnsAllButTheFirstItem(
        string values,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(values);

        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("tail", Delta3Harness.IntsOf(values))))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task Head_OfANode_ReturnsTheNode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document("<a><b/><c/></a>");
        var a = Delta3Harness.Child(document, 0);

        var result = Delta3Harness.Call("head", Delta3Harness.Nodes(a, Delta3Harness.Child(a, 1)));

        await Assert.That(Delta3Harness.IsNodes(result, a)).IsTrue();
    }

    [Test]
    public async Task OutermostAndInnermost_ReturnTheNodesWithoutAncestorAndWithoutDescendantInDocumentOrder(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document("<a><b><c/></b><d/></a>");
        var a = Delta3Harness.Child(document, 0);
        var b = Delta3Harness.Child(a, 0);
        var c = Delta3Harness.Child(b, 0);
        var d = Delta3Harness.Child(a, 1);
        var input = Delta3Harness.Nodes(d, c, c, a, b);

        await Assert.That(Delta3Harness.IsNodes(Delta3Harness.Call("outermost", input), a)).IsTrue();
        await Assert.That(Delta3Harness.IsNodes(Delta3Harness.Call("innermost", input), c, d)).IsTrue();
        await Assert
            .That(Delta3Harness.IsNodes(Delta3Harness.Call("outermost", Delta3Harness.Nodes(d, c)), c, d))
            .IsTrue();
    }

    [Test]
    public async Task OutermostAndInnermost_ForAnAttribute_FollowTheAncestorAndDescendantAxes(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document("<a x=\"1\"><b/></a>");
        var a = Delta3Harness.Child(document, 0);
        var x = Delta3Harness.Attribute(a, 0);
        var input = Delta3Harness.Nodes(x, a);

        // The element is an ancestor of its attribute, but the attribute is not a descendant of the element.
        await Assert.That(Delta3Harness.IsNodes(Delta3Harness.Call("outermost", input), a)).IsTrue();
        await Assert.That(Delta3Harness.IsNodes(Delta3Harness.Call("innermost", input), a, x)).IsTrue();
    }

    [Test]
    [Arguments("innermost")]
    [Arguments("outermost")]
    public async Task InnermostAndOutermost_EmptyAndSeveralTrees_AreHandled(
        string local,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var first = Delta3Harness.Child(Delta3Harness.Document("<a/>"), 0);
        var second = Delta3Harness.Child(Delta3Harness.Document("<b/>"), 0);

        await Assert.That(Delta3Harness.Call(local, XdmSequence.Empty).Count).IsEqualTo(0);
        await Assert.That(Delta3Harness.Call(local, Delta3Harness.Nodes(first, second)).Count).IsEqualTo(2);
        await Assert.That(Delta3Harness.ErrorOf(local, Delta3Harness.Ints(1))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Path_ForEveryKindOfNode_ReturnsTheSpecifiedForm(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document(
            "<a xmlns=\"urn:x\" xmlns:p=\"urn:p\"><b/><b id=\"1\" p:k=\"2\"/>text<!--c--><?pi d?><p:e/><?pi d?></a>"
        );
        var a = Delta3Harness.Child(document, 0);
        var b2 = Delta3Harness.Child(a, 1);
        const string Prefix = "/Q{urn:x}a[1]";

        await Assert.That(Path(document)).IsEqualTo("/");
        await Assert.That(Path(a)).IsEqualTo(Prefix);
        await Assert.That(Path(Delta3Harness.Child(a, 0))).IsEqualTo(Prefix + "/Q{urn:x}b[1]");
        await Assert.That(Path(b2)).IsEqualTo(Prefix + "/Q{urn:x}b[2]");
        await Assert.That(Path(Delta3Harness.Attribute(b2, 0))).IsEqualTo(Prefix + "/Q{urn:x}b[2]/@id");
        await Assert.That(Path(Delta3Harness.Attribute(b2, 1))).IsEqualTo(Prefix + "/Q{urn:x}b[2]/@Q{urn:p}k");
        await Assert.That(Path(Delta3Harness.Child(a, 2))).IsEqualTo(Prefix + "/text()[1]");
        await Assert.That(Path(Delta3Harness.Child(a, 3))).IsEqualTo(Prefix + "/comment()[1]");
        await Assert.That(Path(Delta3Harness.Child(a, 4))).IsEqualTo(Prefix + "/processing-instruction(pi)[1]");
        await Assert.That(Path(Delta3Harness.Child(a, 5))).IsEqualTo(Prefix + "/Q{urn:p}e[1]");
        await Assert.That(Path(Delta3Harness.Child(a, 6))).IsEqualTo(Prefix + "/processing-instruction(pi)[2]");
    }

    [Test]
    public async Task Path_ForANamespaceNode_UsesTheNamespaceAxis(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var a = Delta3Harness.Child(Delta3Harness.Document("<a xmlns:p=\"urn:p\" xmlns=\"urn:d\"/>"), 0);
        var paths = new System.Collections.Generic.List<string>();
        for (var ns = a.Node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
        {
            paths.Add(Path(new XPathNode(ns)));
        }

        await Assert.That(paths).Contains("/Q{urn:d}a[1]/namespace::p");
        await Assert
            .That(paths)
            .Contains("/Q{urn:d}a[1]/namespace::*[Q{http://www.w3.org/2005/xpath-functions}local-name()=\"\"]");
    }

    [Test]
    public async Task Path_ForNodesWithoutNamespace_UsesTheEmptyNamespaceInBraces(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document("<a><b/></a>");

        await Assert.That(Path(Delta3Harness.Child(Delta3Harness.Child(document, 0), 0))).IsEqualTo("/Q{}a[1]/Q{}b[1]");
    }

    [Test]
    public async Task Path_ForATreeWithoutADocumentNode_StartsWithTheRootFunction(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var builder = new XmlTreeBuilder();
        var names = builder.Names;
        builder.StartElement(names.Intern("", "r", ""), default);
        builder.EndAttributes();
        builder.StartElement(names.Intern("", "k", ""), default);
        builder.EndAttributes();
        builder.EndElement(names.Intern("", "k", ""));
        builder.EndElement(names.Intern("", "r", ""));
        var root = new XPathNode(builder.Complete().Document);
        var child = Delta3Harness.Child(root, 0);

        await Assert.That(Path(root)).IsEqualTo("Q{http://www.w3.org/2005/xpath-functions}root()");
        await Assert.That(Path(child)).IsEqualTo("Q{http://www.w3.org/2005/xpath-functions}root()/Q{}k[1]");
    }

    [Test]
    public async Task Path_ForTheEmptySequence_ReturnsTheEmptySequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.Call("path", XdmSequence.Empty).Count).IsEqualTo(0);
        await Assert.That(Delta3Harness.ErrorOf("path", Delta3Harness.Ints(1))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Path_WithoutAnArgument_UsesTheContextItem(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var a = Delta3Harness.Child(Delta3Harness.Document("<a/>"), 0);

        var result = Delta3Harness.CallWithContextItem("path", XdmItem.FromNode(in a));

        await Assert.That(Delta3Harness.Show(result)).IsEqualTo("/Q{}a[1]");
        await Assert.That(Delta3Harness.ErrorWithContextItem("path", null)).IsEqualTo("XPDY0002");
        await Assert.That(Delta3Harness.ErrorWithContextItem("path", XdmItem.FromInteger(1))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task HasChildren_ReportsWhetherTheNodeHasChildren(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = Delta3Harness.Document("<a x=\"1\"><b/>t</a>");
        var a = Delta3Harness.Child(document, 0);

        await Assert.That(Has(document)).IsEqualTo("true");
        await Assert.That(Has(a)).IsEqualTo("true");
        await Assert.That(Has(Delta3Harness.Child(a, 0))).IsEqualTo("false");
        await Assert.That(Has(Delta3Harness.Child(a, 1))).IsEqualTo("false");
        await Assert.That(Has(Delta3Harness.Attribute(a, 0))).IsEqualTo("false");
        await Assert.That(Delta3Harness.Show(Delta3Harness.Call("has-children", XdmSequence.Empty))).IsEqualTo("false");
        await Assert.That(Delta3Harness.ErrorOf("has-children", Delta3Harness.Ints(1))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task HasChildren_WithoutAnArgument_UsesTheContextItem(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var a = Delta3Harness.Child(Delta3Harness.Document("<a><b/></a>"), 0);

        var result = Delta3Harness.CallWithContextItem("has-children", XdmItem.FromNode(in a));

        await Assert.That(Delta3Harness.Show(result)).IsEqualTo("true");
        await Assert.That(Delta3Harness.ErrorWithContextItem("has-children", null)).IsEqualTo("XPDY0002");
    }

    private static string Path(XPathNode node) =>
        Delta3Harness.Show(Delta3Harness.Call("path", Delta3Harness.Nodes(node)));

    private static string Has(XPathNode node) =>
        Delta3Harness.Show(Delta3Harness.Call("has-children", Delta3Harness.Nodes(node)));
}
