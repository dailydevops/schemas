namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using System.Buffers;
using System.Collections.Generic;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

public class DocumentOrderTests
{
    [Test]
    public async Task SortDistinct_SeededRandomTrees_EqualsNaiveSortAndDistinct()
    {
        var random = new Random(20260101);
        var mismatches = 0;
        for (var run = 0; run < 1_000; run++)
        {
            var (all, picked) = RandomCase(random);
            var source = new ArraySource(picked);

            using var sorted = XPathDocumentOrder.SortDistinct(ref source, null);

            var expected = Naive(all, picked);
            var actual = sorted.Nodes.ToArray();
            if (actual.Length != expected.Length)
            {
                mismatches++;
                continue;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (actual[i] != expected[i])
                {
                    mismatches++;
                    break;
                }
            }
        }

        await Assert.That(mismatches).IsEqualTo(0);
    }

    [Test]
    public async Task SortDistinct_NodesOfSeveralTrees_OrdersByTreeThenPosition()
    {
        var (allA, _) = RandomCase(new Random(1));
        var (allB, _) = RandomCase(new Random(2));
        var source = new ArraySource([allB[2], allA[1], allB[0], allA[1], allA[0]]);

        using var sorted = XPathDocumentOrder.SortDistinct(ref source, null);

        var actual = sorted.Nodes.ToArray();
        await Assert.That(actual.Length).IsEqualTo(4);
        for (var i = 1; i < actual.Length; i++)
        {
            await Assert.That(actual[i - 1].CompareDocumentOrder(actual[i])).IsLessThan(0);
        }
    }

    [Test]
    public async Task SortDistinct_EmptyAndRepeatedSingleSource_ReturnThem()
    {
        var tree = new CountingTree(3);
        var empty = new ArraySource([]);
        var single = new ArraySource([tree.At(2), tree.At(2)]);

        using var none = XPathDocumentOrder.SortDistinct(ref empty, null);
        using var one = XPathDocumentOrder.SortDistinct(ref single, null);

        await Assert.That(none.Nodes.Length).IsEqualTo(0);
        await Assert.That(one.Nodes.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Normalize_StaticallyOrderedAndDuplicateFree_NeverCallsTheSort()
    {
        var tree = new CountingTree(50);
        var nodes = new XPathNode[50];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = tree.At(i + 1);
        }

        var properties = XPathOrderAnalysis.Compose(XPathOrderAnalysis.SingleNode, [Axis.Child]);
        var requirement = XPathOrderAnalysis.GetRequirement(properties);
        var source = new ArraySource(nodes);

        using var result = XPathDocumentOrder.Normalize(ref source, requirement, null);

        await Assert.That(requirement).IsEqualTo(XPathOrderRequirement.None);
        await Assert.That(tree.Comparisons).IsEqualTo(0);
        await Assert.That(result.Nodes.Length).IsEqualTo(50);
    }

    [Test]
    public async Task Normalize_Sort_CallsTheSort()
    {
        var tree = new CountingTree(10);
        var source = new ArraySource([tree.At(5), tree.At(2), tree.At(5), tree.At(1)]);

        using var result = XPathDocumentOrder.Normalize(ref source, XPathOrderRequirement.Sort, null);

        await Assert.That(tree.Comparisons).IsGreaterThan(0);
        await Assert.That(result.Nodes.Length).IsEqualTo(3);
    }

    [Test]
    public async Task Normalize_Reverse_OnlyReversesWithoutComparing()
    {
        var tree = new CountingTree(10);
        var source = new ArraySource([tree.At(9), tree.At(7), tree.At(3), tree.At(1)]);

        using var result = XPathDocumentOrder.Normalize(ref source, XPathOrderRequirement.Reverse, null);

        var nodes = result.Nodes.ToArray();
        await Assert.That(tree.Comparisons).IsEqualTo(0);
        await Assert.That(nodes[0] == tree.At(1)).IsTrue();
        await Assert.That(nodes[3] == tree.At(9)).IsTrue();
    }

    [Test]
    public async Task Normalize_Deduplicate_DropsAdjacentDuplicatesWithoutComparing()
    {
        var tree = new CountingTree(10);
        var source = new ArraySource([tree.At(1), tree.At(1), tree.At(4), tree.At(4), tree.At(4), tree.At(6)]);

        using var result = XPathDocumentOrder.Normalize(ref source, XPathOrderRequirement.Deduplicate, null);

        await Assert.That(tree.Comparisons).IsEqualTo(0);
        await Assert.That(result.Nodes.Length).IsEqualTo(3);
    }

    [Test]
    public async Task SortDistinct_Success_ReturnsEveryRentedBufferAfterDispose()
    {
        var pool = new TrackingPool();
        var tree = new CountingTree(2_000);
        var nodes = new XPathNode[1_500];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = tree.At(((i * 7919) % 2_000) + 1);
        }

        var source = new ArraySource(nodes);
        using (var result = XPathDocumentOrder.SortDistinct(ref source, pool))
        {
            await Assert.That(result.Nodes.Length).IsGreaterThan(0);
            await Assert.That(pool.Outstanding).IsGreaterThan(0);
        }

        await Assert.That(pool.Rented).IsGreaterThan(0);
        await Assert.That(pool.Outstanding).IsEqualTo(0);
    }

    [Test]
    [Arguments(0)]
    [Arguments(3)]
    [Arguments(600)]
    public async Task SortDistinct_SourceThrowsMidway_ReturnsEveryRentedBuffer(int throwAt)
    {
        var pool = new TrackingPool();
        var tree = new CountingTree(1_000);
        var nodes = new XPathNode[900];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = tree.At(900 - i);
        }

        var thrown = false;
        try
        {
            var source = new ArraySource(nodes, throwAt);
            using var result = XPathDocumentOrder.SortDistinct(ref source, pool);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
        await Assert.That(pool.Rented).IsGreaterThan(0);
        await Assert.That(pool.Outstanding).IsEqualTo(0);
    }

    [Test]
    public async Task SortDistinct_ComparisonThrows_ReturnsEveryRentedBuffer()
    {
        var pool = new TrackingPool();
        var tree = new ThrowingCompareTree(5);

        var thrown = false;
        try
        {
            var source = new ArraySource([tree.At(3), tree.At(2), tree.At(1)]);
            using var result = XPathDocumentOrder.SortDistinct(ref source, pool);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        await Assert.That(thrown).IsTrue();
        await Assert.That(pool.Outstanding).IsEqualTo(0);
    }

    [Test]
    public async Task SortDistinct_ThousandNodesWithWarmPool_AllocatesZeroBytes()
    {
        var pool = ArrayPool<XPathNode>.Create();
        var tree = new CountingTree(1_000);
        var nodes = new XPathNode[1_000];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = tree.At(((i * 7919) % 1_000) + 1);
        }

        var length = 0;

        AllocationAssert.NoAllocation(() =>
        {
            var source = new ArraySource(nodes);
            using var result = XPathDocumentOrder.SortDistinct(ref source, pool);
            length = result.Nodes.Length;
        });

        await Assert.That(length).IsEqualTo(1_000);
    }

    // A random tree of elements (some with an attribute, some with text) and a random pick of its nodes with repeats.
    private static (XPathNode[] All, XPathNode[] Picked) RandomCase(Random random)
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var name = b.Names.Intern("", "n", "");
        var attribute = b.Names.Intern("", "a", "");
        var depth = 0;
        var budget = random.Next(1, 40);
        do
        {
            if (budget > 0 && (depth == 0 || (depth < 6 && random.Next(3) != 0)))
            {
                b.StartElement(name, default);
                if (random.Next(2) == 0)
                {
                    b.Attribute(attribute, "v");
                }

                b.EndAttributes();
                if (random.Next(2) == 0)
                {
                    b.Text("t", XmlTextKind.Text);
                }

                depth++;
                budget--;
            }
            else
            {
                b.EndElement(name);
                depth--;
            }
        } while (depth > 0);

        b.EndDocument();
        var tree = b.Complete();

        var walk = new List<XPathNode>();
        Walk(new XPathNode(tree.Document), walk);
        var picked = new XPathNode[random.Next(0, (walk.Count * 2) + 1)];
        for (var i = 0; i < picked.Length; i++)
        {
            picked[i] = walk[random.Next(walk.Count)];
        }

        return (walk.ToArray(), picked);
    }

    private static void Walk(XPathNode node, List<XPathNode> into)
    {
        into.Add(node);
        for (var a = node.FirstAttribute(); !a.IsEmpty(); a = a.NextAttribute())
        {
            into.Add(a);
        }

        for (var c = node.FirstChild(); !c.IsEmpty(); c = c.NextSibling())
        {
            Walk(c, into);
        }
    }

    // The reference: distinct by identity, ordered by position in a depth-first walk, which is the definition of document order.
    private static XPathNode[] Naive(XPathNode[] all, XPathNode[] picked)
    {
        var result = new List<XPathNode>();
        foreach (var candidate in all)
        {
            foreach (var p in picked)
            {
                if (p == candidate)
                {
                    result.Add(candidate);
                    break;
                }
            }
        }

        return result.ToArray();
    }
}
