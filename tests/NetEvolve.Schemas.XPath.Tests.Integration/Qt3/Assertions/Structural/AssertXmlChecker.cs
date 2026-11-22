namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using System.Buffers;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xml.Writer;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Evaluated;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Checks assert-xml. The result and the expected XML are each pumped into the canonical XML writer inside a wrapper element, and the
/// two texts are compared for equality, which is the canonical comparison of the catalog schema.
/// </summary>
/// <remarks>
/// <para>
/// A sequence is serialized as with <c>method="xml"</c>: a document node as its children, adjacent atomic values separated by a single
/// space, and an attribute or namespace node cannot be serialized, so it fails the case. The string form of an atomic value is the
/// <c>fn:string</c> of the engine. The expected XML, inline or from the file attribute, is a fragment that is parsed in the wrapper element.
/// </para>
/// <para>
/// Limitations: the canonical form of the writer is a documented subset of the W3C form, so a case can fail on a detail the subset does
/// not cover. With <c>ignore-prefixes</c> both sides are written through <see cref="PrefixlessSink"/>, which ignores prefixes and
/// namespace declarations alike.
/// </para>
/// </remarks>
internal static class AssertXmlChecker
{
    private const int MaxDistinctNames = 1_000_000;
    private const int MaxReason = 200;

    private static readonly XmlWriterOptions Canonical = new(canonical: true);

    public static AssertionVerdict Check(Qt3Case testCase, IQt3Engine engine, EngineResult actual)
    {
        var assertion = testCase.Assertion;
        var items = actual.Items;
        var strings = AtomicStrings(testCase, engine, actual, out var failure);
        if (strings is null)
        {
            return failure;
        }

        XmlDocumentTree expectedTree;
        try
        {
            expectedTree = ParseExpected(assertion);
        }
        catch (Exception exception) when (exception is XmlParseException or IOException)
        {
            return new(CaseOutcome.Fail, "assert-xml: the expected XML cannot be read: " + exception.Message);
        }

        try
        {
            var expected = Serialize(assertion.IgnorePrefixes, sink =>
                WriteChildren(expectedTree.Document.FirstChild, sink)
            );
            var serialized = Serialize(assertion.IgnorePrefixes, sink => WriteResult(items, strings, sink));
            return string.Equals(expected, serialized, StringComparison.Ordinal)
                ? new(CaseOutcome.Pass)
                : new(
                    CaseOutcome.Fail,
                    $"assert-xml: expected '{Abbreviate(expected)}' but got '{Abbreviate(serialized)}'"
                );
        }
        catch (XmlWriteException exception)
        {
            return new(CaseOutcome.Fail, "assert-xml: a serialization error was raised: " + exception.Message);
        }
    }

    // The string form of every item, for the atomic values; null with the failure when an item cannot be serialized.
    private static string[]? AtomicStrings(
        Qt3Case testCase,
        IQt3Engine engine,
        EngineResult actual,
        out AssertionVerdict failure
    )
    {
        failure = default;
        var items = actual.Items;
        var strings = new string[items.Count];
        var atomic = false;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is not XdmItem { IsNode: true } item)
            {
                atomic = true;
            }
            else if (Node(item).Kind is var kind and (XmlNodeKind.Attribute or XmlNodeKind.Namespace))
            {
                failure = new(CaseOutcome.Fail, $"assert-xml: a {kind} node cannot be serialized (SENR0001)");
                return null;
            }
        }

        if (!atomic)
        {
            return strings;
        }

        var answer = EvaluatedAssertionChecker.Ask(testCase, engine, actual, "for $r in $result return string($r)");
        if (answer is not { Kind: EngineResultKind.Value } || answer.Items.Count != items.Count)
        {
            failure = new(CaseOutcome.Fail, EvaluatedAssertionChecker.Describe(testCase.Assertion, answer));
            return null;
        }
        for (var i = 0; i < strings.Length; i++)
        {
            strings[i] = answer.Items[i] as string ?? string.Empty;
        }
        return strings;
    }

    private static XmlDocumentTree ParseExpected(Qt3Assertion assertion) =>
        XmlDocumentLoader.Load(
            "<w>" + ExpectedText(assertion) + "</w>",
            XmlReaderOptions.Default.WithDtdProcessing(true)
        );

    // The inline text, or the text of the file; a file that starts with an XML declaration is a document, so the declaration and the
    // white space around the root element go, and any other file is a fragment that is compared as it is.
    private static string ExpectedText(Qt3Assertion assertion)
    {
        if (assertion.File is not { } file)
        {
            return assertion.Value;
        }

        var text = File.ReadAllText(file);
        if (!text.StartsWith("<?xml", StringComparison.Ordinal))
        {
            return text;
        }

        var end = text.IndexOf("?>", StringComparison.Ordinal);
        return end < 0 ? text : text[(end + 2)..].Trim();
    }

    private static string Serialize(bool ignorePrefixes, Action<IXmlSink> content)
    {
        var output = new ArrayBufferWriter<char>();
        using var writer = new XmlWriter(output, new SpanInterningNameTable(MaxDistinctNames), Canonical);
        IXmlSink sink = ignorePrefixes ? new PrefixlessSink(writer) : writer;
        var declaration = new XmlDeclaration("1.0", string.Empty, null);
        sink.StartDocument(in declaration);
        var wrapper = sink.Names.Intern(string.Empty, "w", string.Empty);
        sink.StartElement(wrapper, default);
        sink.EndAttributes();
        content(sink);
        sink.EndElement(wrapper);
        sink.EndDocument();
        return output.WrittenSpan.ToString();
    }

    private static void WriteChildren(XmlNode container, IXmlSink sink)
    {
        for (var node = container.FirstChild; !node.IsEmpty; node = node.NextSibling)
        {
            XmlTreePump.Pump(node, sink);
        }
    }

    private static void WriteResult(IReadOnlyList<object> items, string[] strings, IXmlSink sink)
    {
        var previousAtomic = false;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is XdmItem { IsNode: true } item)
            {
                previousAtomic = false;
                var node = Node(item);
                if (node.Kind == XmlNodeKind.Document)
                {
                    WriteChildren(node, sink);
                }
                else
                {
                    XmlTreePump.Pump(node, sink);
                }
                continue;
            }

            if (previousAtomic)
            {
                sink.Text(" ", XmlTextKind.Text);
            }
            previousAtomic = true;
            if (strings[i].Length > 0)
            {
                sink.Text(strings[i], XmlTextKind.Text);
            }
        }
    }

    private static XmlNode Node(XdmItem item) => item.TryGetNode(out var node) ? node.Node : default;

    private static string Abbreviate(string text) => text.Length <= MaxReason ? text : text[..MaxReason] + "...";
}
