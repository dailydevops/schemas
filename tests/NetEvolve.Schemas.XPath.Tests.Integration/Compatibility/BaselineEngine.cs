namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.XPath;

/// <summary>
/// The baseline of the differential tests: System.Xml.XPath of the runtime the tests run on (XPath 1.0). Only the tests of
/// the designated leg call it; the other legs read the recorded expectations. The results are rendered by <see cref="ResultFormat"/>.
/// </summary>
internal static class BaselineEngine
{
    /// <summary>Evaluates an expression with the root node of the document as the context.</summary>
    /// <param name="document">The document key.</param>
    /// <param name="expression">The expression.</param>
    /// <returns>The rendered result, or <c>error:</c> and the name of the exception type.</returns>
    public static string Evaluate(string document, string expression)
    {
        try
        {
            using var source = new StringReader(CompatibilityDocuments.Text(document));
            using var reader = XmlReader.Create(
                source,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
            );
            var xml = new XPathDocument(reader);
            var navigator = xml.CreateNavigator();
            var positions = Positions(navigator);
            var result = navigator.Evaluate(expression);
            return result switch
            {
                double number => ResultFormat.Number(number),
                string text => ResultFormat.Text(text),
                bool flag => ResultFormat.Boolean(flag),
                XPathNodeIterator nodes => Nodes(nodes, positions),
                _ => "other:" + result.GetType().Name,
            };
        }
        catch (XPathException exception)
        {
            return "error:" + exception.GetType().Name;
        }
    }

    private static string Nodes(XPathNodeIterator nodes, List<XPathNavigator> positions)
    {
        var labels = new List<string>();
        foreach (XPathNavigator node in nodes)
        {
            labels.Add(Label(node, positions));
        }

        return ResultFormat.Nodes(labels);
    }

    private static string Label(XPathNavigator node, List<XPathNavigator> positions)
    {
        var position = positions.FindIndex(candidate => candidate.IsSamePosition(node));
        var (kind, name) = node.NodeType switch
        {
            XPathNodeType.Root => ('r', string.Empty),
            XPathNodeType.Element => ('e', node.LocalName),
            XPathNodeType.Attribute => ('a', node.LocalName),
            XPathNodeType.Text or XPathNodeType.SignificantWhitespace or XPathNodeType.Whitespace => (
                't',
                string.Empty
            ),
            XPathNodeType.Comment => ('c', string.Empty),
            XPathNodeType.ProcessingInstruction => ('p', node.LocalName),
            _ => ('?', string.Empty),
        };
        return ResultFormat.Label(kind, name, position);
    }

    // The nodes in document order, the root first, and the attributes of an element before its children.
    private static List<XPathNavigator> Positions(XPathNavigator root)
    {
        var positions = new List<XPathNavigator>();
        Walk(root.Clone(), positions);
        return positions;
    }

    private static void Walk(XPathNavigator node, List<XPathNavigator> positions)
    {
        positions.Add(node.Clone());
        if (node.MoveToFirstAttribute())
        {
            do
            {
                positions.Add(node.Clone());
            } while (node.MoveToNextAttribute());

            _ = node.MoveToParent();
        }

        if (node.MoveToFirstChild())
        {
            do
            {
                Walk(node.Clone(), positions);
            } while (node.MoveToNext());

            _ = node.MoveToParent();
        }
    }
}
