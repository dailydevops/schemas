namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Parsing;

/// <summary>Renders a tree as a compact expression, with the group numbers, laziness, anchors, and back references the XPath dialect adds.</summary>
internal static class XPathTreeDump
{
    internal static string Of(RegexTree tree)
    {
        var builder = new StringBuilder();
        Write(builder, tree, tree.Root);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, RegexTree tree, int index)
    {
        var node = tree.Nodes[index];
        switch (node.Kind)
        {
            case RegexNodeKind.Empty:
                builder.Append('E');
                break;
            case RegexNodeKind.Character:
                builder.Append('c').Append(node.Value.ToString("X", CultureInfo.InvariantCulture));
                break;
            case RegexNodeKind.AnyChar:
                builder.Append('.');
                break;
            case RegexNodeKind.Set:
                builder.Append("set").Append(node.Value);
                break;
            case RegexNodeKind.Concat:
                WriteChildren(builder, tree, "cat", node);
                break;
            case RegexNodeKind.Alternation:
                WriteChildren(builder, tree, "alt", node);
                break;
            case RegexNodeKind.Group:
                WriteChildren(
                    builder,
                    tree,
                    node.Value == RegexNode.NoGroup ? "ng" : "g" + node.Value.ToString(CultureInfo.InvariantCulture),
                    node
                );
                break;
            case RegexNodeKind.Repeat:
                builder.Append("rep{").Append(node.Min).Append(',').Append(node.Max).Append('}');
                builder.Append(node.IsLazy ? "?(" : "(");
                Write(builder, tree, node.FirstChild);
                builder.Append(')');
                break;
            case RegexNodeKind.Anchor:
                builder.Append(node.Value == (int)RegexAnchor.Start ? '^' : '$');
                break;
            default:
                builder.Append('\\').Append(node.Value);
                break;
        }
    }

    private static void WriteChildren(StringBuilder builder, RegexTree tree, string name, RegexNode node)
    {
        builder.Append(name).Append('(');
        for (var child = node.FirstChild; child != RegexNode.NoNode; child = tree.Nodes[child].NextSibling)
        {
            if (child != node.FirstChild)
            {
                builder.Append(' ');
            }

            Write(builder, tree, child);
        }

        builder.Append(')');
    }
}
