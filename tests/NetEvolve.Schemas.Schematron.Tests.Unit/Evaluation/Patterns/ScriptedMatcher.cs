namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// A fake pattern matcher with scripted contexts. <c>/</c> is the document node, <c>*</c> any element, <c>name</c> an element,
/// <c>@*</c> any attribute, <c>@name</c> an attribute, <c>text()</c> a text node, and <c>node()</c> an element, text, comment, or
/// processing instruction. <c>err:name</c> is an element test that fails with a dynamic error for that element. A suffix
/// <c>[$name]</c> makes the context read that variable. A context that contains <c>invalid</c> does not compile.
/// </summary>
internal sealed class ScriptedMatcher(List<string> log) : IPatternMatcher
{
    internal int MatchCalls { get; private set; }

    /// <summary>Gets or sets a value indicating whether every call is written to the log; switched off by the allocation tests.</summary>
    internal bool Record { get; set; } = true;

    internal Action<int>? OnMatch { get; set; }

    internal List<(string Text, PatternUse Use, QueryStaticScope Scope)> Compiled { get; } = [];

    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    )
    {
        var text = pattern.ToString();
        Compiled.Add((text, use, scope));
        compiled = null!;
        problem = default;
        if (text.Contains("invalid", StringComparison.Ordinal))
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.PatternUnsupported,
                "unsupported pattern",
                FakeModel.Origin,
                "node-test"
            );
            return false;
        }

        var slots = new List<int>();
        var core = text;
        var bracket = text.IndexOf("[$", StringComparison.Ordinal);
        if (bracket >= 0)
        {
            core = text.Substring(0, bracket);
            var name = text.Substring(bracket + 2, text.Length - bracket - 3);
            for (var slot = scope.VariableNames.Count - 1; slot >= 0; slot--)
            {
                if (string.Equals(scope.VariableNames[slot], name, StringComparison.Ordinal))
                {
                    slots.Add(slot);
                    break;
                }
            }
        }

        compiled = new ScriptedPattern(core, Kinds(core), [.. slots]);
        return true;
    }

    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched)
    {
        MatchCalls++;
        OnMatch?.Invoke(MatchCalls);
        var text = ((ScriptedPattern)compiled).Text;
        var node = candidate.Node;
        if (Record)
        {
            log.Add("match:" + text);
        }
        matched = false;
        if (text.StartsWith("err:", StringComparison.Ordinal))
        {
            if (
                node.Kind == XmlNodeKind.Element
                && string.Equals(Local(node), text.Substring(4), StringComparison.Ordinal)
            )
            {
                ((PatternTestRun)run).Raise("XPDY0002");
                return QueryStatus.Problem;
            }

            return QueryStatus.Ok;
        }

        matched = text switch
        {
            "/" => node.Kind == XmlNodeKind.Document,
            "*" => node.Kind == XmlNodeKind.Element,
            "@*" => node.Kind == XmlNodeKind.Attribute,
            "text()" => node.Kind == XmlNodeKind.Text,
            "node()" => node.Kind
                is XmlNodeKind.Element
                    or XmlNodeKind.Text
                    or XmlNodeKind.Comment
                    or XmlNodeKind.ProcessingInstruction,
            _ when text is ['@', ..] => node.Kind == XmlNodeKind.Attribute
                && string.Equals(Local(node), text.Substring(1), StringComparison.Ordinal),
            _ => node.Kind == XmlNodeKind.Element && string.Equals(Local(node), text, StringComparison.Ordinal),
        };
        return QueryStatus.Ok;
    }

    private static string Local(NetEvolve.Schemas.Xml.Contracts.XmlNode node) =>
        node.Tree!.Names.GetString(node.Name.Local);

    private static QueryNodeKinds Kinds(string text) =>
        text switch
        {
            "/" => QueryNodeKinds.Document,
            "@*" => QueryNodeKinds.Attribute,
            "text()" => QueryNodeKinds.Text,
            "node()" => QueryNodeKinds.Element
                | QueryNodeKinds.Text
                | QueryNodeKinds.Comment
                | QueryNodeKinds.ProcessingInstruction,
            _ when text is ['@', ..] => QueryNodeKinds.Attribute,
            _ => QueryNodeKinds.Element,
        };

    private sealed class ScriptedPattern(string text, QueryNodeKinds kinds, int[] slots)
        : CompiledPattern(kinds, Array.Empty<XPathQName>())
    {
        internal string Text { get; } = text;

        internal override ReadOnlySpan<int> ReferencedSlots => slots;
    }
}
