namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The node comparisons is, &lt;&lt;, and &gt;&gt; (XPath 2.0, 3.5.3) on fake nodes.</summary>
public class NodeComparisonTests
{
    private static readonly string[] Operators = ["is", "<<", ">>"];

    private static XdmSequence Node(CountingTree tree, int index) =>
        XdmSequence.Create(XdmItem.FromNode(tree.At(index)));

    private static XdmSequence Nodes(CountingTree tree, params int[] indexes)
    {
        var items = new XdmItem[indexes.Length];
        for (var i = 0; i < indexes.Length; i++)
        {
            items[i] = XdmItem.FromNode(tree.At(indexes[i]));
        }

        return XdmSequence.Create(items);
    }

    [Test]
    public async Task Is_IsTrueForTheSameNode_AndFalseForAnother()
    {
        var tree = new CountingTree(5);

        await Assert.That(Show(Run(Operator("is"), Node(tree, 2), Node(tree, 2)))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("is"), Node(tree, 2), Node(tree, 3)))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("is"), Node(tree, 0), Node(tree, 0)))).IsEqualTo("true");
    }

    [Test]
    public async Task Is_ComparesIdentity_NotTheValueOfTheNodes()
    {
        // Both elements of the fake tree have the empty string value, and are still two nodes.
        var tree = new CountingTree(2);
        var other = new CountingTree(2);

        await Assert.That(Show(Run(Operator("is"), Node(tree, 1), Node(tree, 2)))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("is"), Node(tree, 1), Node(other, 1)))).IsEqualTo("false");
    }

    [Test]
    public async Task Precedes_IsTrueWhenTheLeftNodeComesFirstInDocumentOrder()
    {
        var tree = new CountingTree(5);

        await Assert.That(Show(Run(Operator("<<"), Node(tree, 1), Node(tree, 4)))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("<<"), Node(tree, 4), Node(tree, 1)))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("<<"), Node(tree, 0), Node(tree, 1)))).IsEqualTo("true");
    }

    [Test]
    public async Task Follows_IsTrueWhenTheLeftNodeComesLaterInDocumentOrder()
    {
        var tree = new CountingTree(5);

        await Assert.That(Show(Run(Operator(">>"), Node(tree, 4), Node(tree, 1)))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator(">>"), Node(tree, 1), Node(tree, 4)))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator(">>"), Node(tree, 1), Node(tree, 0)))).IsEqualTo("true");
    }

    [Test]
    public async Task ANodeDoesNotPrecedeOrFollowItself()
    {
        var tree = new CountingTree(3);

        await Assert.That(Show(Run(Operator("<<"), Node(tree, 2), Node(tree, 2)))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator(">>"), Node(tree, 2), Node(tree, 2)))).IsEqualTo("false");
    }

    [Test]
    public async Task NodesOfTwoTrees_HaveOneStableOrder_AndPrecedesAndFollowsAgree()
    {
        var first = new CountingTree(2);
        var second = new CountingTree(2);

        var firstBeforeSecond = Show(Run(Operator("<<"), Node(first, 1), Node(second, 1)));
        var secondBeforeFirst = Show(Run(Operator("<<"), Node(second, 1), Node(first, 1)));

        await Assert.That(firstBeforeSecond).IsNotEqualTo(secondBeforeFirst);
        await Assert.That(Show(Run(Operator(">>"), Node(second, 1), Node(first, 1)))).IsEqualTo(firstBeforeSecond);
        await Assert.That(Show(Run(Operator("<<"), Node(first, 1), Node(second, 2)))).IsEqualTo(firstBeforeSecond);
        await Assert.That(Show(Run(Operator("<<"), Node(first, 2), Node(second, 1)))).IsEqualTo(firstBeforeSecond);
    }

    [Test]
    [Arguments("is")]
    [Arguments("<<")]
    [Arguments(">>")]
    public async Task EmptyOperand_YieldsTheEmptySequence(string op)
    {
        var tree = new CountingTree(2);
        var comparison = Operator(op);

        await Assert.That(Show(Run(comparison, XdmSequence.Empty, Node(tree, 1)))).IsEqualTo("()");
        await Assert.That(Show(Run(comparison, Node(tree, 1), XdmSequence.Empty))).IsEqualTo("()");
        await Assert.That(Show(Run(comparison, XdmSequence.Empty, XdmSequence.Empty))).IsEqualTo("()");
    }

    [Test]
    [Arguments("is")]
    [Arguments("<<")]
    [Arguments(">>")]
    public async Task EmptyOperand_WinsOverAnOperandThatIsNotASingleNode(string op)
    {
        // XPath 2.0, 3.5.3 lists the empty sequence before the single node requirement.
        var tree = new CountingTree(3);
        var comparison = Operator(op);

        await Assert.That(Show(Run(comparison, XdmSequence.Empty, Nodes(tree, 1, 2)))).IsEqualTo("()");
        await Assert.That(Show(Run(comparison, Nodes(tree, 1, 2), XdmSequence.Empty))).IsEqualTo("()");
        await Assert.That(Show(Run(comparison, XdmSequence.Empty, One("i:1")))).IsEqualTo("()");
    }

    [Test]
    [Arguments("is")]
    [Arguments("<<")]
    [Arguments(">>")]
    public async Task MoreThanOneNode_RaisesXpty0004(string op)
    {
        var tree = new CountingTree(3);
        var comparison = Operator(op);

        await Assert.That(Outcome(() => Run(comparison, Nodes(tree, 1, 2), Node(tree, 3)))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(comparison, Node(tree, 3), Nodes(tree, 1, 2)))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(comparison, Nodes(tree, 1, 2), Nodes(tree, 1, 2)))).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("is")]
    [Arguments("<<")]
    [Arguments(">>")]
    public async Task AnAtomicOperand_RaisesXpty0004(string op)
    {
        var tree = new CountingTree(3);
        var comparison = Operator(op);

        await Assert.That(Outcome(() => Run(comparison, One("i:1"), Node(tree, 1)))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(comparison, Node(tree, 1), One("s:a")))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(comparison, One("u:a"), One("u:a")))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TheKindsOfNodes_AreComparedByIdentityAndOrderAlike()
    {
        var source = new FakeNodeSource();
        var element = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Element)));
        var attribute = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Attribute)));
        var text = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Text)));

        await Assert.That(Show(Run(Operator("is"), attribute, attribute))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("is"), element, text))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("<<"), element, attribute))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator(">>"), text, element))).IsEqualTo("true");
    }

    [Test]
    public async Task TheResult_IsABooleanItem()
    {
        var tree = new CountingTree(2);

        foreach (var op in Operators)
        {
            var result = Run(Operator(op), Node(tree, 1), Node(tree, 2));

            await Assert.That(result.Count).IsEqualTo(1);
            await Assert.That(result[0].TypeId).IsEqualTo(XdmTypeId.Boolean);
        }
    }

    [Test]
    public async Task TheComparisonOfDocumentOrder_IsAskedOncePerEvaluation()
    {
        var tree = new CountingTree(3);

        _ = Run(Operator("<<"), Node(tree, 1), Node(tree, 2));
        _ = Run(Operator("is"), Node(tree, 1), Node(tree, 2));

        // The identity test needs no order, and the order test needs one comparison.
        await Assert.That(tree.Comparisons).IsEqualTo(1);
    }
}
