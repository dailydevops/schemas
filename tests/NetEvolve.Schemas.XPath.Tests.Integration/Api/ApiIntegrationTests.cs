namespace NetEvolve.Schemas.XPath.Tests.Integration.Api;

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class ApiIntegrationTests
{
    private const string Shop =
        "<shop><item id=\"i1\" price=\"10\"><name>Pen</name></item>"
        + "<item id=\"i2\" price=\"25.5\"><name>Book</name></item>"
        + "<item id=\"i3\" price=\"7\"><name>Cup</name></item><total>42.5</total></shop>";

    private static readonly string[] Expressions =
    [
        "count(/shop/item)",
        "/shop/item/name",
        "string(/shop/item[@id = 'i2']/name)",
        "sum(/shop/item/@price)",
        "/shop/item[@price > 8]/@id",
        "for $i in /shop/item return string-length($i/name)",
        "if (/shop/total > 40) then 'big' else 'small'",
        "/shop/item[last()]/name",
        "(1 to 5)[. mod 2 = 1]",
        "some $p in /shop/item/@price satisfies $p > 20",
    ];

    private static XPathStaticContext Statics() =>
        XPathStaticContext.Default.ToBuilder().WithNamespace("xs", "http://www.w3.org/2001/XMLSchema").Build();

    private static string Render(in XdmSequence value)
    {
        var text = new StringBuilder();
        for (var i = 0; i < value.Count; i++)
        {
            var item = value[i];
            _ = text.Append(i > 0 ? "|" : string.Empty);
            if (item.TryGetNode(out var node))
            {
                _ = text.Append(node.Node.StringValue);
            }
            else if (item.TryGetBoolean(out var boolean))
            {
                _ = text.Append(boolean ? "true" : "false");
            }
            else if (item.TryGetInteger(out var integer))
            {
                _ = text.Append(integer.ToString(CultureInfo.InvariantCulture));
            }
            else if (item.TryGetDouble(out var number))
            {
                _ = text.Append(number.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (item.TryGetString(out var value2))
            {
                _ = text.Append(value2);
            }
            else
            {
                _ = text.Append(item.Kind);
            }
        }

        return text.ToString();
    }

    private static string Evaluate(XPathExpression expression, XmlDocumentTree document)
    {
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(document.Document)));
        return Render(expression.Evaluate(context));
    }

    private static ReadOnlySequence<byte> Segmented(byte[] bytes)
    {
        var half = bytes.Length / 2;
        var first = new Segment(bytes.AsMemory(0, half), 0);
        var last = first.Append(bytes.AsMemory(half));
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Test]
    public async Task Compile_StringAndSpan_GiveEqualResults()
    {
        var document = XmlDocumentLoader.Load(Shop.AsSpan());
        foreach (var text in Expressions)
        {
            var fromString = XPathExpression.Compile(text, Statics());
            var fromSpan = XPathExpression.TryCompile(text.AsSpan(), Statics()).Expression!;

            await Assert.That(fromSpan.Source).IsEqualTo(fromString.Source);
            await Assert.That(Evaluate(fromSpan, document)).IsEqualTo(Evaluate(fromString, document));
        }
    }

    [Test]
    public async Task Evaluate_DocumentsFromStreamMemoryAndSequence_GiveEqualResults(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Encoding.UTF8.GetBytes(Shop);
        var fromMemory = XmlDocumentLoader.LoadFromMemory(new ReadOnlyMemory<byte>(bytes));
        var fromSequence = XmlDocumentLoader.Load(Segmented(bytes));
        using var stream = new MemoryStream(bytes);
        var fromStream = await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken);

        foreach (var text in Expressions)
        {
            var expression = XPathExpression.Compile(text, Statics());

            var expected = Evaluate(expression, fromMemory);

            await Assert.That(expected).IsNotEmpty();
            await Assert.That(Evaluate(expression, fromStream)).IsEqualTo(expected);
            await Assert.That(Evaluate(expression, fromSequence)).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Evaluate_OneExpressionFromSixteenTasks_GivesTheResultOfEachDocument()
    {
        var concat = XPathExpression.Compile("concat(string(/r/v), ':', count(/r/w), ':', /r/v * 2)", Statics());
        var documents = Enumerable
            .Range(0, 16)
            .Select(i =>
                XmlDocumentLoader.Load(
                    ("<r><v>" + i + "</v>" + string.Concat(Enumerable.Repeat("<w/>", i)) + "</r>").AsSpan()
                )
            )
            .ToArray();

        var results = await Task.WhenAll(
            documents.Select(document =>
                Task.Run(() =>
                {
                    var last = string.Empty;
                    for (var round = 0; round < 200; round++)
                    {
                        last = Evaluate(concat, document);
                    }

                    return last;
                })
            )
        );

        for (var i = 0; i < 16; i++)
        {
            await Assert.That(results[i]).IsEqualTo(i + ":" + i + ":" + (i * 2));
        }
    }

    [Test]
    public async Task HostFunction_OverridesTheBuiltInFunction_AndTheSecondLayerOverridesTheFirst()
    {
        var document = XmlDocumentLoader.Load(Shop.AsSpan());
        var name = new XPathQName("http://www.w3.org/2005/xpath-functions", "count");
        var first = new XPathFunctionLibrary("first", [new Fixed(name, 1, 1, 100)]);
        var second = new XPathFunctionLibrary("second", [new Fixed(name, 1, 1, 200)]);
        var builtIn = Statics();
        var one = builtIn.ToBuilder().WithLibrary(first).Build();
        var two = builtIn.ToBuilder().WithLibrary(first).WithLibrary(second).Build();

        var text = "count(/shop/item)";

        await Assert.That(Evaluate(XPathExpression.Compile(text, builtIn), document)).IsEqualTo("3");
        await Assert.That(Evaluate(XPathExpression.Compile(text, one), document)).IsEqualTo("100");
        await Assert.That(Evaluate(XPathExpression.Compile(text, two), document)).IsEqualTo("200");
    }

    [Test]
    public async Task Compile_OnceAndEvaluateWithVariablesPerContext_GivesTheValueOfEachContext()
    {
        var x = new XPathQName(string.Empty, "x");
        var expression = XPathExpression.Compile(
            "$x * 2",
            XPathStaticContext.Default.ToBuilder().WithVariable(x).Build()
        );

        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(i =>
                    Task.Run(() =>
                    {
                        using var context = new XPathEvaluationContext();
                        context.Variables.Set(x, XdmItem.FromInteger(i));
                        return Render(expression.Evaluate(context));
                    })
                )
        );

        for (var i = 0; i < 16; i++)
        {
            await Assert.That(results[i]).IsEqualTo((i * 2).ToString(CultureInfo.InvariantCulture));
        }
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }

    private sealed class Fixed(XPathQName name, int minArity, int maxArity, long value)
        : XPathHostFunction(name, minArity, maxArity)
    {
        public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromInteger(value));
            return XPathEvaluationStatus.Ok;
        }
    }
}
