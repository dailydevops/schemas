namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A call whose only argument is a path folds over the pooled item list: same results and errors, no item array per call.</summary>
public class CallNodePathArgumentTests
{
    private static string Xml(int children, string text = "1")
    {
        var xml = new StringBuilder("<r>");
        for (var i = 0; i < children; i++)
        {
            _ = xml.Append("<x>").Append(text).Append("</x>");
        }

        return xml.Append("</r>").ToString();
    }

    private static string Run(string expression, string xml)
    {
        var tree = XmlDocumentLoader.Load(xml.AsSpan());
        var result = XPathEngine.TryCompile(
            expression,
            expression,
            null,
            XPathEngine.AllSlots,
            XPathOptimizations.None
        );
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(new XmlNode(tree, 0))));
        try
        {
            return SequencesHarness.Show(result.Expression!.Compiled.Evaluate(context));
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    [Test]
    [Arguments("count(/r/x)", "<r><x>1</x><x>2</x><x>3</x></r>", "integer:3")]
    [Arguments("count(/r/y)", "<r><x>1</x></r>", "integer:0")]
    [Arguments("exists(/r/x)", "<r><x>1</x><x>2</x></r>", "boolean:true")]
    [Arguments("exists(/r/y)", "<r><x>1</x></r>", "boolean:false")]
    [Arguments("empty(/r/y)", "<r><x>1</x></r>", "boolean:true")]
    [Arguments("empty(/r/x)", "<r><x>1</x></r>", "boolean:false")]
    [Arguments("not(/r/x)", "<r><x>1</x><x>2</x></r>", "boolean:false")]
    [Arguments("not(/r/y)", "<r><x>1</x></r>", "boolean:true")]
    [Arguments("boolean(/r/x)", "<r><x>1</x><x>2</x></r>", "boolean:true")]
    [Arguments("boolean(/r/y)", "<r><x>1</x></r>", "boolean:false")]
    [Arguments("sum(/r/x)", "<r><x>1</x><x>2.5</x><x>3</x></r>", "double:6.5")]
    [Arguments("sum(/r/y)", "<r><x>1</x></r>", "integer:0")]
    [Arguments("avg(/r/x)", "<r><x>1</x><x>2</x></r>", "double:1.5")]
    [Arguments("avg(/r/y)", "<r><x>1</x></r>", "")]
    [Arguments("min(/r/x)", "<r><x>3</x><x>1</x><x>2</x></r>", "double:1")]
    [Arguments("max(/r/x)", "<r><x>3</x><x>1</x><x>2</x></r>", "double:3")]
    [Arguments("max(/r/x)", "<r><x>a</x><x>b</x></r>", "FORG0001")]
    [Arguments("sum(/r/x)", "<r><x>1</x><x>a</x></r>", "FORG0001")]
    [Arguments("avg(/r/x)", "<r><x>a</x></r>", "FORG0001")]
    [Arguments("sum(/r/x | /r/z)", "<r><x>1</x><z>4</z></r>", "double:5")]
    [Arguments("count(/r/x | /r/z)", "<r><x>1</x><z>4</z></r>", "integer:2")]
    public async Task Call_OverAPath_GivesTheResultOfTheGenericCall(string expression, string xml, string expected)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(xml);

        await Assert.That(Run(expression, xml)).IsEqualTo(expected);
    }

    private static long BytesPerCall(string expression, string xml)
    {
        var tree = XmlDocumentLoader.Load(xml.AsSpan());
        var result = XPathEngine.TryCompile(
            expression,
            expression,
            null,
            XPathEngine.AllSlots,
            XPathOptimizations.None
        );
        var compiled = result.Expression!.Compiled;
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(new XmlNode(tree, 0))));
        for (var i = 0; i < 32; i++)
        {
            _ = compiled.Evaluate(context);
        }

        const int Rounds = 64;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Rounds; i++)
        {
            _ = compiled.Evaluate(context);
        }

        return (GC.GetAllocatedBytesForCurrentThread() - before) / Rounds;
    }

    // The path result array alone is 24 + 24n bytes (n = 40 here).
    [Test]
    [Arguments("count(/r/x)")]
    [Arguments("exists(/r/x)")]
    public async Task Call_OverAPath_AllocatesNoItemArray(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var bytes = BytesPerCall(expression, Xml(40));

        await Assert.That(bytes).IsLessThan(300);
    }

    // The string value of each node stays (about 24 bytes per node); the path array and the atomization array (3912 bytes together) go.
    [Test]
    [Arguments("sum(/r/x)", 1200)]
    [Arguments("max(/r/x)", 2200)]
    public async Task Call_OverAPathOfNodes_AllocatesNoItemArrays(string expression, int budget)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var bytes = BytesPerCall(expression, Xml(40));

        await Assert.That(bytes).IsLessThan(budget);
    }

    // The comma operator makes the argument a node that is no item list producer, so that call takes the generic route.
    [Test]
    [Arguments("sum", "/r/x/string()")]
    [Arguments("boolean", "/r/x/string()")]
    [Arguments("boolean", "/r/x/number()")]
    [Arguments("max", "/r/x/string()")]
    [Arguments("min", "/r/x/number()")]
    public async Task Call_OverAPath_GivesTheResultOfTheGenericRoute(string function, string path)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(path);

        var xml = "<r><x>1</x><x>2</x></r>";

        await Assert.That(Run($"{function}({path})", xml)).IsEqualTo(Run($"{function}(({path}, ()))", xml));
    }
}
