namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Parsing;

using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;

/// <summary>Renders a tree as a compact expression so a test can assert its exact shape.</summary>
internal static class RegexTreeDump
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
                builder.Append('c').Append(node.Value.ToString("X", System.Globalization.CultureInfo.InvariantCulture));
                break;
            case RegexNodeKind.AnyChar:
                builder.Append('.');
                break;
            case RegexNodeKind.Set:
                WriteSet(builder, tree.GetSet(node.Value));
                break;
            case RegexNodeKind.Concat:
                WriteChildren(builder, tree, "cat", node);
                break;
            case RegexNodeKind.Alternation:
                WriteChildren(builder, tree, "alt", node);
                break;
            case RegexNodeKind.Group:
                WriteChildren(builder, tree, "grp", node);
                break;
            case RegexNodeKind.Repeat:
                builder.Append("rep{").Append(node.Min).Append(',').Append(node.Max).Append("}(");
                Write(builder, tree, node.FirstChild);
                builder.Append(')');
                break;
            default:
                builder.Append(node.Kind);
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

    private static void WriteSet(StringBuilder builder, RegexCharacterSet set)
    {
        builder.Append('[');
        var ranges = RangesOf(set);
        for (var i = 0; i < ranges.Count; i += 2)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(ranges[i].ToString("X", System.Globalization.CultureInfo.InvariantCulture));
            if (ranges[i + 1] != ranges[i])
            {
                builder
                    .Append('-')
                    .Append(ranges[i + 1].ToString("X", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        builder.Append(']');
    }

    // RegexCharacterSet exposes only Contains, so recover the range pairs by scanning every code point.
    private static List<int> RangesOf(RegexCharacterSet set)
    {
        var ranges = new List<int>();
        var start = -1;
        for (var cp = 0; cp <= 0x10FFFF; cp++)
        {
            var inSet = set.Contains(cp);
            if (inSet && start < 0)
            {
                start = cp;
            }
            else if (!inSet && start >= 0)
            {
                ranges.Add(start);
                ranges.Add(cp - 1);
                start = -1;
            }
        }

        if (start >= 0)
        {
            ranges.Add(start);
            ranges.Add(0x10FFFF);
        }

        return ranges;
    }
}
