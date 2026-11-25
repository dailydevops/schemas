namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Delta3;

using System;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Calls the 3.0 and 3.1 functions of the <c>fn</c> namespace through the default catalog, the way a compiled call does.</summary>
internal static class Delta3Harness
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    internal static bool TryResolve(string local, int arity, out XPathFunction? function, XPathVersion version) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, local),
            arity,
            XPathHostRequirements.None,
            out function
        );

    internal static XdmSequence Call(string local, params XdmSequence[] arguments) => CallCore(local, null, arguments);

    internal static XdmSequence CallWithContextItem(string local, XdmItem item, params XdmSequence[] arguments) =>
        CallCore(local, item, arguments);

    private static XdmSequence CallCore(string local, XdmItem? contextItem, XdmSequence[] arguments)
    {
        if (!TryResolve(local, arguments.Length, out var function, XPathVersion.V31))
        {
            throw new InvalidOperationException($"fn:{local}#{arguments.Length} is not registered.");
        }

        using var context = new XPathEvaluationContext();
        if (contextItem is { } item)
        {
            context.SetContextItem(item);
        }

        return function!.Call(context, arguments);
    }

    internal static string ErrorOf(string local, params XdmSequence[] arguments)
    {
        try
        {
            _ = Call(local, arguments);
            return "none";
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    internal static string ErrorWithContextItem(string local, XdmItem? item)
    {
        try
        {
            _ = CallCore(local, item, []);
            return "none";
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    internal static XdmSequence Str(params string[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromString(values[i]);
        }

        return XdmSequence.Create(items);
    }

    internal static XdmSequence Ints(params long[] values)
    {
        var items = new XdmItem[values.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromInteger(values[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Builds the integers of a space-separated list.</summary>
    internal static XdmSequence IntsOf(string list) =>
        Ints([.. list.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(long.Parse)]);

    internal static XdmSequence Nodes(params XPathNode[] nodes)
    {
        var items = new XdmItem[nodes.Length];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = XdmItem.FromNode(in nodes[i]);
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Renders a sequence: strings and integers by value, booleans as <see langword="true"/> or <see langword="false"/>, nodes by kind, items joined by <c>|</c>.</summary>
    internal static string Show(XdmSequence sequence)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            if (i > 0)
            {
                _ = builder.Append('|');
            }

            var item = sequence[i];
            if (item.TryGetString(out var text))
            {
                _ = builder.Append(text);
            }
            else if (item.TryGetInteger(out var number))
            {
                _ = builder.Append(number);
            }
            else if (item.TryGetBoolean(out var flag))
            {
                _ = builder.Append(flag ? "true" : "false");
            }
            else
            {
                _ = item.TryGetNode(out var node);
                _ = builder.Append(node.Kind);
            }
        }

        return builder.ToString();
    }

    internal static XPathNode Document(string xml) => new(XmlDocumentLoader.LoadFromMemory(xml.AsMemory()).Document);

    /// <summary>Gets the child at a zero-based position.</summary>
    internal static XPathNode Child(XPathNode parent, int index)
    {
        var node = parent.Node.FirstChild;
        for (var i = 0; i < index; i++)
        {
            node = node.NextSibling;
        }

        return new XPathNode(node);
    }

    /// <summary>Gets the attribute at a zero-based position.</summary>
    internal static XPathNode Attribute(XPathNode element, int index)
    {
        var node = element.Node.FirstAttribute;
        for (var i = 0; i < index; i++)
        {
            node = node.NextAttribute;
        }

        return new XPathNode(node);
    }

    /// <summary>Tells whether a sequence holds exactly the given nodes, in that order.</summary>
    internal static bool IsNodes(XdmSequence sequence, params XPathNode[] expected)
    {
        if (sequence.Count != expected.Length)
        {
            return false;
        }

        for (var i = 0; i < expected.Length; i++)
        {
            if (!sequence[i].TryGetNode(out var node) || !node.IsSameNode(expected[i]))
            {
                return false;
            }
        }

        return true;
    }
}
