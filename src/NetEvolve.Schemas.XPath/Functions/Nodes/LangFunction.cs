namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:lang</c>: whether the language of a node, or of the context item without a node argument, is the tested language or one of
/// its sublanguages. The language is the value of the nearest <c>xml:lang</c> attribute on the node or on an ancestor, whether or
/// not it matches; without one the result is false. The match ignores case and accepts a language that continues the tested one after a hyphen.
/// </summary>
/// <remarks>Limitation: the value of <c>xml:lang</c> is compared as written; a value with surrounding white space does not match, though the type <c>xs:language</c> would collapse it.</remarks>
internal sealed class LangFunction : NodeFunction
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    /// <summary>Initializes a new instance of the <see cref="LangFunction"/> class.</summary>
    public LangFunction()
        : base(
            "lang",
            1,
            2,
            [
                new XPathParameterType(XPathItemType.String, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.Node, Occurrence.ExactlyOne),
            ]
        ) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var test = string.Empty;
        var argument = invocation.Arguments[0];
        if (!argument.IsEmpty)
        {
            _ = argument[0].TryGetString(out var text);
            test = text!;
        }

        // The second parameter is exactly one node, so a node is always found.
        _ = TryGetNode(ref invocation, 1, out var node);
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(Matches(node, test)));
        return XPathEvaluationStatus.Ok;
    }

    private static bool Matches(XPathNode node, ReadOnlySpan<char> test)
    {
        for (
            var current = node.Kind == XdmNodeKind.Element ? node : node.Parent();
            !current.IsEmpty();
            current = current.Parent()
        )
        {
            if (current.Kind == XdmNodeKind.Element && TryMatch(current, test, out var matches))
            {
                return matches;
            }
        }

        return false;
    }

    private static bool TryMatch(XPathNode element, ReadOnlySpan<char> test, out bool matches)
    {
        var names = element.Tree!.Names;
        for (var attribute = element.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
        {
            var name = attribute.Name();
            if (
                string.Equals(names.GetString(name.Local), "lang", StringComparison.Ordinal)
                && string.Equals(names.GetString(name.NamespaceUri), XmlNamespace, StringComparison.Ordinal)
            )
            {
                matches = Covers(
                    attribute.TryGetStringValueSpan(out var span) ? span : attribute.StringValue().AsSpan(),
                    test
                );
                return true;
            }
        }

        matches = false;
        return false;
    }

    private static bool Covers(ReadOnlySpan<char> language, ReadOnlySpan<char> test) =>
        language.Length >= test.Length
        && language.Slice(0, test.Length).Equals(test, StringComparison.OrdinalIgnoreCase)
        && (language.Length == test.Length || language[test.Length] == '-');
}
