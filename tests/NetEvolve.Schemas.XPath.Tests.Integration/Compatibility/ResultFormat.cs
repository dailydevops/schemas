namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The one rendering of a result that the library and the baseline share, so that a difference of the type system does not show as a
/// difference of the result. A number is <c>number:</c> and its shortest round-trip form (<c>NaN</c>, <c>INF</c>, <c>-INF</c>, and
/// <c>-0</c> are written out), a string is <c>string:</c>, a boolean is <c>boolean:true</c> or <c>boolean:false</c>, and a node-set is
/// <c>nodes:</c> and its nodes in document order, each as the kind letter (<c>r</c> root, <c>e</c> element, <c>a</c> attribute,
/// <c>t</c> text, <c>c</c> comment, <c>p</c> processing instruction), the local name, and the position in document order of the whole
/// document, root first.
/// </summary>
internal static class ResultFormat
{
    public static string Number(double value) =>
        double.IsNaN(value) ? "number:NaN"
        : double.IsPositiveInfinity(value) ? "number:INF"
        : double.IsNegativeInfinity(value) ? "number:-INF"
        : value == 0 ? (1 / value < 0 ? "number:-0" : "number:0")
        : "number:" + value.ToString("R", CultureInfo.InvariantCulture);

    public static string Text(string value) => "string:" + value;

    public static string Boolean(bool value) => value ? "boolean:true" : "boolean:false";

    public static string Nodes(IEnumerable<string> labels) => "nodes:" + string.Join(" ", labels);

    public static string Label(char kind, string localName, int position) =>
        kind.ToString() + ":" + localName + "#" + position.ToString(CultureInfo.InvariantCulture);

    /// <summary>Renders a result of the library.</summary>
    /// <param name="result">The result.</param>
    /// <param name="root">The document node of the context, which numbers the nodes.</param>
    /// <returns>The rendered result.</returns>
    public static string Render(in XdmSequence result, XPathNode root)
    {
        if (result.Count == 1 && !result[0].IsNode)
        {
            var item = result[0];
            if (item.TryGetBoolean(out var flag))
            {
                return Boolean(flag);
            }

            if (item.TryGetString(out var text))
            {
                return Text(text);
            }

            // namespace-uri() returns an xs:anyURI, where the baseline has a string.
            if (item.TryGetAnyUri(out var uri))
            {
                return Text(uri!);
            }

            return NumericPromotion.TryToDouble(item, out var number)
                ? Number(number)
                : "other:" + item.Kind.ToString();
        }

        var positions = Positions(root);
        var labels = new List<string>(result.Count);
        for (var i = 0; i < result.Count; i++)
        {
            if (!result[i].TryGetNode(out var node))
            {
                return "other:" + result[i].Kind.ToString();
            }

            labels.Add(Label(Kind(node.Kind), LocalName(node), positions[node]));
        }

        return Nodes(labels);
    }

    private static Dictionary<XPathNode, int> Positions(XPathNode root)
    {
        var positions = new Dictionary<XPathNode, int>();
        Walk(root, positions);
        return positions;
    }

    private static void Walk(XPathNode node, Dictionary<XPathNode, int> positions)
    {
        positions.Add(node, positions.Count);
        for (var attribute = node.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
        {
            positions.Add(attribute, positions.Count);
        }

        for (var child = node.FirstChild(); !child.IsEmpty(); child = child.NextSibling())
        {
            Walk(child, positions);
        }
    }

    private static char Kind(XdmNodeKind kind) =>
        kind switch
        {
            XdmNodeKind.Document => 'r',
            XdmNodeKind.Element => 'e',
            XdmNodeKind.Attribute => 'a',
            XdmNodeKind.Text => 't',
            XdmNodeKind.Comment => 'c',
            XdmNodeKind.ProcessingInstruction => 'p',
            _ => '?',
        };

    private static string LocalName(XPathNode node) =>
        node.Kind is XdmNodeKind.Element or XdmNodeKind.Attribute or XdmNodeKind.ProcessingInstruction
            ? node.Tree!.Names.GetString(node.Name().Local)
            : string.Empty;
}
