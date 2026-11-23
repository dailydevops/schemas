namespace NetEvolve.Schemas.Benchmarks.XPath.Core;

using System.Xml.XPath;
using BenchmarkDotNet.Attributes;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Walk of the descendant axis over the whole invoice, against a <c>System.Xml.XPath</c> navigator that visits every node.
/// The walk allocates nothing after warm-up.
/// </summary>
#pragma warning disable CA1515 // BenchmarkDotNet requires public benchmark classes
public class XPathAxisWalkBenchmarks
#pragma warning restore CA1515
{
    private readonly XPathNode _root;
    private readonly XPathNodeMatcher _matcher;
    private readonly XPathNavigator _navigator = XPathBenchmarkDocument.CreateNavigator();

    /// <summary>Initializes a new instance of the <see cref="XPathAxisWalkBenchmarks"/> class.</summary>
    public XPathAxisWalkBenchmarks()
    {
        var tree = XPathBenchmarkDocument.LoadTree();
        _root = XPathBenchmarkDocument.Root(tree);
        _matcher = XPathNodeTest.AnyNode.Bind(tree.Names);
    }

    /// <summary>Proves that both walks visit the same nodes.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var expected = WalkDescendantsSystemXml();
        var actual = WalkDescendants();
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"The walks disagree: System.Xml.XPath visits {expected} nodes, this library {actual}."
            );
        }
    }

    /// <summary>Baseline: the navigator descends through the document without a node-set.</summary>
    /// <returns>The number of visited nodes.</returns>
    [Benchmark(Baseline = true)]
    public int WalkDescendantsSystemXml()
    {
        var nav = _navigator;
        nav.MoveToRoot();
        var count = 0;
        var depth = 0;
        while (true)
        {
            if (nav.MoveToFirstChild())
            {
                depth++;
                count++;
                continue;
            }

            while (!nav.MoveToNext())
            {
                if (depth == 0)
                {
                    return count;
                }

                _ = nav.MoveToParent();
                depth--;
            }

            count++;
        }
    }

    /// <summary>Walks the descendant axis from the document node.</summary>
    /// <returns>The number of visited nodes.</returns>
    [Benchmark]
    public int WalkDescendants()
    {
        var count = 0;
        var iterator = new XPathAxisIterator(_root, Axis.Descendant, _matcher);
        while (iterator.MoveNext())
        {
            count++;
        }

        return count;
    }
}
