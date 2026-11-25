namespace NetEvolve.Schemas.XPath.Functions.AnalyzeString;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Regex;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:analyze-string</c> (Functions and Operators 3.1, 5.6.5): the segments of the input that the pattern divides, as the element
/// <c>analyze-string-result</c> with the children <c>match</c> and <c>non-match</c>; a <c>match</c> holds its text and the nested
/// elements named group with the attribute <c>nr</c>. The string value of the result is the input. The empty input, and the empty
/// sequence, give an element without children; a pattern that matches the zero-length string is FORX0003, also for the empty input.
/// </summary>
/// <remarks>
/// Limitations: the result elements are untyped (DEF-40, #52); the namespace nodes of the result are the default namespace only; an
/// empty group that lies at the end of an enclosing group is a sibling after it, because the bounds alone do not tell the two apart;
/// the node count of the result is bounded by the input length, not by a limit of the context. The tree is built by the
/// <see cref="XmlTreeBuilder"/> from the segments of the <see cref="MatchSegmentEnumerator"/>.
/// </remarks>
internal sealed class AnalyzeStringFunction : RegexFunction
{
    private const string Namespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Initializes a new instance of the <see cref="AnalyzeStringFunction"/> class.</summary>
    /// <param name="cache">The cache of the compiled patterns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
    public AnalyzeStringFunction(PatternCache cache)
        : base("analyze-string", 2, 3, [OptionalString, OneString, OneString], cache) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var operations = Compile(invocation.Arguments, 2);
        if (operations.MatchesEmptyString)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORX0003);
        }

        var input = Text(invocation.Arguments[0]);
        using var builder = new XmlTreeBuilder(trackPositions: false);
        var names = new Names(builder.Names);
        var root = names.Name("analyze-string-result");
        builder.StartElement(root, default);
        builder.NamespaceDeclaration(default, root.NamespaceUri);
        builder.EndAttributes();

        var segments = new MatchSegmentEnumerator(operations, input.AsSpan());
        try
        {
            while (segments.MoveNext())
            {
                var segment = segments.Current;
                if (segment.Kind == MatchSegmentKind.Match)
                {
                    WriteMatch(builder, names, segment);
                }
                else
                {
                    var name = names.Name("non-match");
                    builder.StartElement(name, default);
                    builder.Text(segment.Text, XmlTextKind.Text);
                    builder.EndElement(name);
                }
            }

            if (segments.IsAborted)
            {
                throw RegexErrors.StepCeiling();
            }
        }
        finally
        {
            segments.Dispose();
        }

        builder.EndElement(root);
        invocation.Result = XdmSequence.Create(XdmItem.FromNode(new XPathNode(builder.Complete().Document)));
        return XPathEvaluationStatus.Ok;
    }

    private static void WriteMatch(XmlTreeBuilder builder, Names names, in MatchSegment segment)
    {
        var match = names.Name("match");
        builder.StartElement(match, default);

        // Groups are numbered by their opening parenthesis, so reading them in order opens enclosing groups first.
        var open = new Stack<(XmlName Name, int End)>();
        var position = 0;
        var text = segment.Text;
        for (var number = 1; number <= segment.GroupCount; number++)
        {
            if (!segment.TryGetGroup(number, out var range))
            {
                continue;
            }

            var start = range.Offset - segment.Index;
            while (open.Count > 0 && open.Peek().End <= start)
            {
                position = Close(builder, open, text, position);
            }

            // A quantified group can keep a capture from an earlier iteration that lies before the current position
            // or outside the enclosing open group. Such a capture cannot be nested in the tree, so it is skipped.
            if (start < position || (open.Count > 0 && start + range.Length > open.Peek().End))
            {
                continue;
            }

            builder.Text(text.Slice(position, start - position), XmlTextKind.Text);
            position = start;
            var group = names.Name("group");
            builder.StartElement(group, default);
            builder.Attribute(names.Name("nr", false), number.ToString(CultureInfo.InvariantCulture).AsSpan());
            builder.EndAttributes();
            open.Push((group, start + range.Length));
        }

        while (open.Count > 0)
        {
            position = Close(builder, open, text, position);
        }

        builder.Text(text.Slice(position), XmlTextKind.Text);
        builder.EndElement(match);
    }

    private static int Close(
        XmlTreeBuilder builder,
        Stack<(XmlName Name, int End)> open,
        ReadOnlySpan<char> text,
        int position
    )
    {
        var (name, end) = open.Pop();
        builder.Text(text.Slice(position, end - position), XmlTextKind.Text);
        builder.EndElement(name);
        return end;
    }

    /// <summary>Interns the names of the result elements in the name table of the builder.</summary>
    private readonly struct Names(XmlNameTable table)
    {
        public XmlName Name(string local, bool inNamespace = true) =>
            new(default, table.Intern(local.AsSpan()), inNamespace ? table.Intern(Namespace.AsSpan()) : default);
    }
}
