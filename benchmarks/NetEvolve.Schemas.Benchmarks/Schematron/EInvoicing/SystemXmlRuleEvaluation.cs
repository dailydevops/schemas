namespace NetEvolve.Schemas.Benchmarks.Schematron.EInvoicing;

using System.Xml;
using System.Xml.XPath;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The baseline of the e-invoicing benchmarks: <c>System.Xml</c> parses the invoice and evaluates the contexts and tests
/// of the rule set with precompiled XPath expressions, once per node the context selects.
/// </summary>
/// <remarks>
/// Limitation: <c>System.Xml</c> implements XPath 1.0 only, while the EN 16931 rules are bound to XPath 2.0 and use
/// variables. The baseline therefore evaluates the rules whose context and tests compile and evaluate in XPath 1.0 on the
/// invoice, which is a subset of the rule set; the library evaluates all rules.
/// </remarks>
internal sealed class SystemXmlRuleEvaluation
{
    private readonly List<(XPathExpression Context, XPathExpression[] Tests)> _rules = [];

    /// <summary>Initializes a new instance of the <see cref="SystemXmlRuleEvaluation"/> class.</summary>
    /// <param name="schema">The expanded schema, whose rules are the source of the contexts and tests.</param>
    /// <param name="document">The invoice used to drop the rules that <c>System.Xml</c> cannot evaluate.</param>
    public SystemXmlRuleEvaluation(SchematronSchema schema, byte[] document)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(document);
        var namespaces = new XmlNamespaceManager(new NameTable());
        foreach (var ns in schema.Namespaces)
        {
            namespaces.AddNamespace(ns.Prefix, ns.NamespaceName);
        }

        var navigator = Parse(document);
        foreach (var rule in schema.Patterns.SelectMany(p => p.Rules).Where(r => r.Context is not null))
        {
            if (!TryCompile("//" + rule.Context, namespaces, out var context))
            {
                continue;
            }

            var tests = new List<XPathExpression>();
            foreach (var test in rule.Items.OfType<SchematronAssertion>().Select(a => a.Test))
            {
                if (TryCompile(test, namespaces, out var expression) && Evaluates(navigator, context, expression))
                {
                    tests.Add(expression);
                }
            }

            if (tests.Count > 0)
            {
                _rules.Add((context, [.. tests]));
            }
        }
    }

    /// <summary>Gets the number of rules the baseline evaluates.</summary>
    public int RuleCount => _rules.Count;

    /// <summary>Parses the invoice and evaluates every rule for every context node.</summary>
    /// <param name="document">The invoice as UTF-8.</param>
    /// <returns>The number of evaluated tests.</returns>
    public int Evaluate(byte[] document)
    {
        var navigator = Parse(document);
        var evaluated = 0;
        foreach (var (context, tests) in _rules)
        {
            var nodes = navigator.Select(context);
            while (nodes.MoveNext())
            {
                foreach (var test in tests)
                {
                    evaluated++;
                    _ = nodes.Current!.Evaluate(test);
                }
            }
        }

        return evaluated;
    }

    private static XPathNavigator Parse(byte[] document)
    {
        using var stream = new MemoryStream(document, writable: false);
        using var reader = XmlReader.Create(
            stream,
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
        );

        return new XPathDocument(reader).CreateNavigator();
    }

    private static bool TryCompile(string xpath, XmlNamespaceManager namespaces, out XPathExpression expression)
    {
        try
        {
            expression = XPathExpression.Compile(xpath, namespaces);
            return true;
        }
        catch (XPathException)
        {
            expression = null!;
            return false;
        }
    }

    private static bool Evaluates(XPathNavigator navigator, XPathExpression context, XPathExpression test)
    {
        try
        {
            var nodes = navigator.Select(context);
            while (nodes.MoveNext())
            {
                _ = nodes.Current!.Evaluate(test);
            }

            return true;
        }
        catch (Exception exception) when (exception is XPathException or XmlException or InvalidOperationException)
        {
            return false;
        }
    }
}
