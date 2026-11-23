namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using System.Xml;
using System.Xml.XPath;

/// <summary>
/// The baseline of the validation benchmarks: <c>System.Xml</c> parses the document and evaluates the same contexts and
/// tests with precompiled XPath expressions, once per node the context selects.
/// </summary>
internal sealed class SystemXmlXPathEvaluation
{
    private readonly XPathExpression _context;
    private readonly XPathExpression[] _tests;

    /// <summary>Initializes a new instance of the <see cref="SystemXmlXPathEvaluation"/> class.</summary>
    /// <param name="ruleCount">The number of rules; each has the same context and its own test.</param>
    public SystemXmlXPathEvaluation(int ruleCount)
    {
        var namespaces = new XmlNamespaceManager(new NameTable());
        namespaces.AddNamespace("i", SchematronBenchmarkInput.Namespace);
        _context = XPathExpression.Compile("//" + SchematronBenchmarkInput.Context, namespaces);
        _tests = new XPathExpression[ruleCount];
        for (var i = 0; i < ruleCount; i++)
        {
            _tests[i] = XPathExpression.Compile(SchematronBenchmarkInput.Test(i), namespaces);
        }
    }

    /// <summary>Parses the document and evaluates every rule for every context node.</summary>
    /// <param name="document">The document as UTF-8.</param>
    /// <returns>The number of fired rules.</returns>
    public int Evaluate(byte[] document)
    {
        using var stream = new MemoryStream(document, writable: false);
        using var reader = XmlReader.Create(
            stream,
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
        );
        var navigator = new XPathDocument(reader).CreateNavigator();
        var fired = 0;
        foreach (var test in _tests)
        {
            var nodes = navigator.Select(_context);
            while (nodes.MoveNext())
            {
                fired++;
                _ = (bool)nodes.Current!.Evaluate(test);
            }
        }

        return fired;
    }
}
