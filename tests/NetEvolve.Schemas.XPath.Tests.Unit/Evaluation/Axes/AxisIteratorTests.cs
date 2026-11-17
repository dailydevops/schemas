namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static AxisFixture;

public class AxisIteratorTests
{
    // Document order of the fixture: / a @id @p:lang b 't1' #c ?pi b 't2' i 'x' 't3' p:e
    [Test]
    [Arguments("child::node()", "doc", "a")]
    [Arguments("child::node()", "a", "b #c ?pi b p:e")]
    [Arguments("child::node()", "b2", "'t2' i 't3'")]
    [Arguments("child::node()", "id", "")]
    [Arguments("child::node()", "t1", "")]
    [Arguments("descendant::node()", "doc", "a b 't1' #c ?pi b 't2' i 'x' 't3' p:e")]
    [Arguments("descendant::node()", "b2", "'t2' i 'x' 't3'")]
    [Arguments("descendant::node()", "id", "")]
    [Arguments("descendant::node()", "t1", "")]
    [Arguments("descendant-or-self::node()", "b2", "b 't2' i 'x' 't3'")]
    [Arguments("descendant-or-self::node()", "id", "@id")]
    [Arguments("descendant-or-self::node()", "t1", "'t1'")]
    [Arguments("self::node()", "a", "a")]
    [Arguments("self::node()", "id", "@id")]
    [Arguments("self::node()", "doc", "/")]
    [Arguments("attribute::node()", "a", "@id @p:lang")]
    [Arguments("attribute::node()", "b1", "")]
    [Arguments("attribute::node()", "doc", "")]
    [Arguments("attribute::node()", "id", "")]
    [Arguments("parent::node()", "i", "b")]
    [Arguments("parent::node()", "id", "a")]
    [Arguments("parent::node()", "a", "/")]
    [Arguments("parent::node()", "doc", "")]
    [Arguments("ancestor::node()", "i", "b a /")]
    [Arguments("ancestor::node()", "lang", "a /")]
    [Arguments("ancestor::node()", "doc", "")]
    [Arguments("ancestor-or-self::node()", "i", "i b a /")]
    [Arguments("ancestor-or-self::node()", "lang", "@p:lang a /")]
    [Arguments("ancestor-or-self::node()", "doc", "/")]
    [Arguments("following-sibling::node()", "b1", "#c ?pi b p:e")]
    [Arguments("following-sibling::node()", "t2", "i 't3'")]
    [Arguments("following-sibling::node()", "e", "")]
    [Arguments("following-sibling::node()", "id", "")]
    [Arguments("following-sibling::node()", "doc", "")]
    [Arguments("preceding-sibling::node()", "b2", "?pi #c b")]
    [Arguments("preceding-sibling::node()", "e", "b ?pi #c b")]
    [Arguments("preceding-sibling::node()", "t3", "i 't2'")]
    [Arguments("preceding-sibling::node()", "b1", "")]
    [Arguments("preceding-sibling::node()", "lang", "")]
    [Arguments("preceding-sibling::node()", "doc", "")]
    [Arguments("following::node()", "b1", "#c ?pi b 't2' i 'x' 't3' p:e")]
    [Arguments("following::node()", "t2", "i 'x' 't3' p:e")]
    [Arguments("following::node()", "i", "'t3' p:e")]
    [Arguments("following::node()", "x", "'t3' p:e")]
    [Arguments("following::node()", "id", "b 't1' #c ?pi b 't2' i 'x' 't3' p:e")]
    [Arguments("following::node()", "lang", "b 't1' #c ?pi b 't2' i 'x' 't3' p:e")]
    [Arguments("following::node()", "e", "")]
    [Arguments("following::node()", "a", "")]
    [Arguments("following::node()", "doc", "")]
    [Arguments("preceding::node()", "e", "'t3' 'x' i 't2' b ?pi #c 't1' b")]
    [Arguments("preceding::node()", "i", "'t2' ?pi #c 't1' b")]
    [Arguments("preceding::node()", "t3", "'x' i 't2' ?pi #c 't1' b")]
    [Arguments("preceding::node()", "b2", "?pi #c 't1' b")]
    [Arguments("preceding::node()", "b1", "")]
    [Arguments("preceding::node()", "id", "")]
    [Arguments("preceding::node()", "lang", "")]
    [Arguments("preceding::node()", "doc", "")]
    public async Task Walk_Axis_VisitsTheNodesInSpecificationOrder(string step, string context, string expected) =>
        await Assert.That(Eval(step, context)).IsEqualTo(expected);

    [Test]
    [Arguments("Child", false)]
    [Arguments("Descendant", false)]
    [Arguments("DescendantOrSelf", false)]
    [Arguments("Attribute", false)]
    [Arguments("Self", false)]
    [Arguments("FollowingSibling", false)]
    [Arguments("Following", false)]
    [Arguments("Parent", false)]
    [Arguments("Ancestor", true)]
    [Arguments("AncestorOrSelf", true)]
    [Arguments("PrecedingSibling", true)]
    [Arguments("Preceding", true)]
    public async Task Properties_Axis_ReportDirectionOrderAndDuplicates(string axisName, bool reverse)
    {
        var properties = XPathAxes.GetProperties(Enum.Parse<Axis>(axisName));

        await Assert.That(properties.HasFlag(XPathAxisProperties.Reverse)).IsEqualTo(reverse);
        await Assert.That(properties.HasFlag(XPathAxisProperties.InDocumentOrder)).IsEqualTo(!reverse);
        await Assert.That(properties.HasFlag(XPathAxisProperties.DuplicateFree)).IsTrue();
    }

    [Test]
    public async Task Create_NamespaceAxis_YieldsXPST0010()
    {
        var step = Parse("namespace::x");
        var context = Node("a");

        var failure = Assert.Throws<XPathException>(() =>
            _ = new XPathAxisIterator(context, step.Axis, XPathNodeTest.AnyNode.Bind(context.Tree!.Names))
        );

        await Assert.That(failure.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0010);
        await Assert.That(() => XPathAxes.GetProperties(Axis.Namespace)).Throws<XPathException>();
    }

    [Test]
    public async Task Walk_ForEach_EnumeratesTheAxis()
    {
        var context = Node("a");
        var count = 0;

        foreach (
            var node in new XPathAxisIterator(context, Axis.Child, XPathNodeTest.AnyNode.Bind(context.Tree!.Names))
        )
        {
            count += node.IsEmpty() ? 0 : 1;
        }

        await Assert.That(count).IsEqualTo(5);
    }
}
