namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the sequence and aggregate functions through the default catalog, the way a compiled call does: resolve by expanded name and
/// arity, then <see cref="XPathFunction.Call"/>. An argument is written <c>kind:payload</c> (several items joined by <c>|</c>), and
/// <c>()</c> is the empty sequence; a result is shown with the type of each item, a node by its path, an error as <c>error:CODE</c>.
/// </summary>
/// <remarks>
/// A node is written <c>node:tree/child/child@attribute</c>: the tree key, the zero-based positions among all children, and an
/// attribute by its local name. The trees are written as XML text in <see cref="Sources"/>; the prefix <c>p</c> is bound to
/// <c>urn:p</c>.
/// </remarks>
internal static class SequencesHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    internal const string Codepoint = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    private const string PrefixNamespace = "urn:p";

    // d1 is the reference; d2 to d9 each differ from it in one respect, and d2 differs only where deep-equal must not look.
    private static readonly Dictionary<string, string> Sources = new(StringComparer.Ordinal)
    {
        ["d1"] = "<r a=\"1\" b=\"2\">hi<x>t</x><y/><!--c--><?pi d?></r>",
        ["d2"] = "<r b=\"2\" a=\"1\">hi<x>t</x><y/></r>",
        ["d3"] = "<r a=\"9\" b=\"2\">hi<x>t</x><y/></r>",
        ["d4"] = "<r a=\"1\" b=\"2\">hi<x>T</x><y/></r>",
        ["d5"] = "<r a=\"1\" b=\"2\">hi<y/><x>t</x></r>",
        ["d6"] = "<r a=\"1\">hi<x>t</x><y/></r>",
        ["d7"] = "<r a=\"1\" c=\"2\">hi<x>t</x><y/></r>",
        ["d8"] = "<p:r a=\"1\" b=\"2\">hi<x>t</x><y/></p:r>",
        ["d9"] = "<r a=\"1\" b=\"2\">hi<x>t</x></r>",
        ["c1"] = "<r><!--c--></r>",
        ["c2"] = "<r><!--z--></r>",
        ["i1"] = "<r><?pi d?></r>",
        ["i2"] = "<r><?pi e?></r>",
        ["i3"] = "<r><?q d?></r>",
        ["n1"] = "<r p:a=\"1\"/>",
        ["n2"] = "<r a=\"1\"/>",
        ["w1"] = "<r><x>1</x><x>2</x></r>",
        ["w2"] = "<r><x>1</x><x>2</x></r>",
        ["deep1"] = Nest(5000),
        ["deep2"] = Nest(5000),
    };

    private static readonly Dictionary<string, XmlDocumentTree> Trees = BuildAll();

    // An element nested to the given depth, to show that nothing here recurses once per level.
    private static string Nest(int depth)
    {
        var text = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            _ = text.Append("<n>");
        }

        _ = text.Append('t');
        for (var i = 0; i < depth; i++)
        {
            _ = text.Append("</n>");
        }

        return text.ToString();
    }

    /// <summary>Resolves a built-in function of the default catalog.</summary>
    internal static bool TryResolve(string local, int arity, out XPathFunction? function) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName(FunctionNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    /// <summary>Calls a function with arguments written as text; a raised error is shown as <c>error:CODE</c>.</summary>
    internal static string Run(string local, params string[] arguments)
    {
        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = Parse(arguments[i]);
        }

        return Run(local, sequences);
    }

    /// <summary>Calls a function with sequences; a raised error is shown as <c>error:CODE</c>.</summary>
    internal static string Run(string local, XdmSequence[] sequences)
    {
        if (!TryResolve(local, sequences.Length, out _))
        {
            return "error:not-registered";
        }

        try
        {
            return Show(CallWith(local, sequences));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>Resolves a function of the default catalog, which must be registered.</summary>
    internal static XPathFunction Resolve(string local, int arity) =>
        TryResolve(local, arity, out var function)
            ? function!
            : throw new InvalidOperationException(local + " is not registered for " + arity + " arguments.");

    /// <summary>Calls a function with sequences that the caller built, in a fresh evaluation context.</summary>
    internal static XdmSequence CallWith(string local, params XdmSequence[] arguments)
    {
        using var context = new XPathEvaluationContext();
        return Resolve(local, arguments.Length).Call(context, arguments);
    }

    /// <summary>Parses a sequence: <c>()</c> is empty, items are joined by <c>|</c>.</summary>
    internal static XdmSequence Parse(string argument)
    {
        if (string.Equals(argument, "()", StringComparison.Ordinal))
        {
            return XdmSequence.Empty;
        }

        var parts = argument.Split('|');
        if (parts.Length == 1)
        {
            return XdmSequence.Create(ParseItem(parts[0]));
        }

        var items = new XdmItem[parts.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = ParseItem(parts[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Parses one item written <c>kind:payload</c>.</summary>
    internal static XdmItem ParseItem(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var kind = text.Substring(0, colon);
        var payload = text.Substring(colon + 1);
        return kind switch
        {
            "node" => XdmItem.FromNode(ResolveNode(payload)),
            "string" => XdmItem.FromString(payload),
            "untypedAtomic" => XdmItem.FromUntypedAtomic(payload),
            "anyURI" => XdmItem.FromAnyUri(payload),
            "boolean" => XdmItem.FromBoolean(string.Equals(payload, "true", StringComparison.Ordinal)),
            "integer" => XdmItem.FromInteger(long.Parse(payload, CultureInfo.InvariantCulture)),
            "decimal" => XdmItem.FromDecimal(XsdDecimal.Parse(payload).Value),
            "double" => XdmItem.FromDouble(ParseFloating(payload)),
            "float" => XdmItem.FromFloat((float)ParseFloating(payload)),
            "date" => XdmItem.FromDateTime(XsdDateTime.Parse(payload, XsdDateTimeKind.Date, XsdVersion.V10).Value),
            "duration" => XdmItem.FromDuration(XsdDuration.Parse(payload, DurationKind.Duration).Value),
            _ => throw new ArgumentException("Unknown item kind " + kind, nameof(text)),
        };
    }

    /// <summary>Shows a result sequence: each item with its type, separated by a comma.</summary>
    internal static string Show(XdmSequence sequence)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
            {
                _ = text.Append(',');
            }

            _ = text.Append(Show(sequence[i]));
        }

        return text.ToString();
    }

    private static string Show(XdmItem item)
    {
        if (item.TryGetNode(out var node))
        {
            return "node:" + PathOf(node);
        }

        if (item.TryGetAnyUri(out var uri))
        {
            return "anyURI:" + uri;
        }

        if (item.TryGetFloat(out var single))
        {
            return "float:" + CastFixture.RenderFloat(single);
        }

        if (item.TryGetDouble(out var wide))
        {
            return "double:" + CastFixture.RenderDouble(wide);
        }

        if (item.TryGetDateTime(out var date))
        {
            return "date:" + date;
        }

        if (item.TryGetDuration(out var duration))
        {
            return "duration:" + duration;
        }

        if (item.TryGetDecimal(out var number))
        {
            return "decimal:" + number;
        }

        if (item.TryGetUntypedAtomic(out var untyped))
        {
            return "untypedAtomic:" + untyped;
        }

        if (item.TryGetString(out var text))
        {
            return "string:" + text;
        }

        if (item.TryGetBoolean(out var boolean) && item.Kind == XdmItemKind.Boolean)
        {
            return boolean ? "boolean:true" : "boolean:false";
        }

        return item.TryGetInteger(out var integer)
            ? "integer:" + integer.ToString(CultureInfo.InvariantCulture)
            : "other:" + item.Kind;
    }

    private static double ParseFloating(string payload) =>
        payload switch
        {
            "INF" => double.PositiveInfinity,
            "-INF" => double.NegativeInfinity,
            "NaN" => double.NaN,
            _ => double.Parse(payload, NumberStyles.Float, CultureInfo.InvariantCulture),
        };

    // The path of a node: the key of its tree, then the positions among all children, and an attribute by its local name.
    private static string PathOf(in XPathNode node)
    {
        if (node.Kind == XdmNodeKind.Document)
        {
            foreach (var entry in Trees)
            {
                if (ReferenceEquals(entry.Value, node.Tree))
                {
                    return entry.Key;
                }
            }

            return "?";
        }

        var parent = node.Parent();
        if (node.Kind == XdmNodeKind.Attribute)
        {
            return PathOf(parent) + "@" + node.Tree!.Names.GetString(node.Name().Local);
        }

        var position = 0;
        for (var sibling = parent.FirstChild(); !sibling.IsEmpty(); sibling = sibling.NextSibling())
        {
            if (sibling.IsSameNode(in node))
            {
                break;
            }

            position++;
        }

        return PathOf(parent) + "/" + position.ToString(CultureInfo.InvariantCulture);
    }

    private static XPathNode ResolveNode(string path)
    {
        var at = path.IndexOf('@', StringComparison.Ordinal);
        var attribute = at < 0 ? null : path.Substring(at + 1);
        var steps = (at < 0 ? path : path.Substring(0, at)).Split('/');
        var node = new XPathNode(Trees[steps[0]].Document);
        for (var i = 1; i < steps.Length; i++)
        {
            node = node.FirstChild();
            for (var skip = int.Parse(steps[i], CultureInfo.InvariantCulture); skip > 0; skip--)
            {
                node = node.NextSibling();
            }
        }

        if (attribute is null)
        {
            return node;
        }

        for (var a = node.FirstAttribute(); !a.IsEmpty(); a = a.NextAttribute())
        {
            if (string.Equals(a.Tree!.Names.GetString(a.Name().Local), attribute, StringComparison.Ordinal))
            {
                return a;
            }
        }

        throw new ArgumentException("No attribute " + attribute + " at " + path, nameof(path));
    }

    private static Dictionary<string, XmlDocumentTree> BuildAll()
    {
        var all = new Dictionary<string, XmlDocumentTree>(StringComparer.Ordinal);
        foreach (var entry in Sources)
        {
            all[entry.Key] = Build(entry.Value);
        }

        return all;
    }

    // A reader for the small XML subset of the sources: elements with attributes, text, comments, and processing instructions.
    private static XmlDocumentTree Build(string xml)
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var open = new Stack<XmlName>();
        var i = 0;
        while (i < xml.Length)
        {
            if (xml[i] != '<')
            {
                var end = xml.IndexOf('<', i, StringComparison.Ordinal);
                b.Text(xml.AsSpan(i, end - i), XmlTextKind.Text);
                i = end;
            }
            else if (string.CompareOrdinal(xml, i, "<!--", 0, 4) == 0)
            {
                var end = xml.IndexOf("-->", i, StringComparison.Ordinal);
                b.Comment(xml.AsSpan(i + 4, end - i - 4));
                i = end + 3;
            }
            else if (string.CompareOrdinal(xml, i, "<?", 0, 2) == 0)
            {
                var end = xml.IndexOf("?>", i, StringComparison.Ordinal);
                var body = xml.AsSpan(i + 2, end - i - 2);
                var space = body.IndexOf(' ');
                b.ProcessingInstruction(b.Names.Intern(body.Slice(0, space)), body.Slice(space + 1));
                i = end + 2;
            }
            else if (xml[i + 1] == '/')
            {
                var end = xml.IndexOf('>', i, StringComparison.Ordinal);
                b.EndElement(open.Pop());
                i = end + 1;
            }
            else
            {
                i = StartTag(b, xml, i, open);
            }
        }

        b.EndDocument();
        return b.Complete();
    }

    private static int StartTag(XmlTreeBuilder b, string xml, int start, Stack<XmlName> open)
    {
        var end = xml.IndexOf('>', start, StringComparison.Ordinal);
        var selfClosing = xml[end - 1] == '/';
        var body = xml.Substring(start + 1, end - start - 1 - (selfClosing ? 1 : 0));
        var parts = body.Split(' ');
        var name = QualifiedName(b, parts[0].AsSpan());
        b.StartElement(name, default);
        if (parts[0].IndexOf(':', StringComparison.Ordinal) > 0 || body.Contains("p:", StringComparison.Ordinal))
        {
            b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern(PrefixNamespace));
        }

        for (var p = 1; p < parts.Length; p++)
        {
            var equals = parts[p].IndexOf('=', StringComparison.Ordinal);
            b.Attribute(
                QualifiedName(b, parts[p].AsSpan(0, equals)),
                parts[p].AsSpan(equals + 2, parts[p].Length - equals - 3)
            );
        }

        b.EndAttributes();
        if (selfClosing)
        {
            b.EndElement(name);
        }
        else
        {
            open.Push(name);
        }

        return end + 1;
    }

    private static XmlName QualifiedName(XmlTreeBuilder b, ReadOnlySpan<char> text)
    {
        var colon = text.IndexOf(':');
        return colon < 0
            ? b.Names.Intern("", text, "")
            : b.Names.Intern(text.Slice(0, colon), text.Slice(colon + 1), PrefixNamespace);
    }
}
