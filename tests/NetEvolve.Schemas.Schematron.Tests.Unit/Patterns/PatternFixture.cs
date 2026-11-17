namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The tree <c>&lt;!--head--&gt;&lt;root id="r" xmlns:p="urn:p"&gt;&lt;a n="1"&gt;t1&lt;b/&gt;&lt;b k="x"/&gt;&lt;!--c--&gt;&lt;?pi d?&gt;&lt;/a&gt;
/// &lt;a n="2"&gt;&lt;b/&gt;&lt;/a&gt;&lt;p:e/&gt;&lt;/root&gt;</c>, a scope that binds <c>p</c>, and helpers that run a pattern over every node.
/// </summary>
internal static class PatternFixture
{
    private static readonly XmlDocumentTree Tree = Build();

    /// <summary>The nodes by key in document order: doc, head, root, rootId, a1, a1n, t1, b1, b2, b2k, c, pi, a2, a2n, a2b, e.</summary>
    public static readonly IReadOnlyList<KeyValuePair<string, XPathNode>> Nodes = Index(Tree);

    public static QueryStaticScope Scope { get; } =
        new([new SchematronNamespace(FakeModel.Origin, "p", "urn:p")], [], null, SchematronEdition.Iso2016);

    public static XmlDocumentTree Document => Tree;

    public static XPathNode Node(string key)
    {
        foreach (var pair in Nodes)
        {
            if (string.Equals(pair.Key, key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        throw new ArgumentException(key, nameof(key));
    }

    /// <summary>Compiles a pattern; the test fails with the problem text when it does not compile.</summary>
    public static CompiledPattern Compile(PatternMatcher matcher, string pattern)
    {
        if (!matcher.TryCompile(pattern, PatternUse.RuleContext, Scope, out var compiled, out var problem))
        {
            throw new InvalidOperationException($"{pattern}: {problem.Code} {problem.Message}");
        }

        return compiled;
    }

    /// <summary>Renders the keys of all nodes the pattern matches, in document order.</summary>
    public static string MatchingKeys(string pattern, PatternTestBinding? binding = null)
    {
        binding ??= new PatternTestBinding();
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, pattern);
        using var run = binding.BeginRun(null!, default);
        var keys = new List<string>();
        foreach (var pair in Nodes)
        {
            var status = matcher.Matches(compiled, run, pair.Value, out var matched);
            if (status != QueryStatus.Ok)
            {
                throw new InvalidOperationException(run.GetProblem().Message);
            }

            if (matched)
            {
                keys.Add(pair.Key);
            }
        }

        return string.Join(" ", keys);
    }

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static XmlDocumentTree Build()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        b.Comment("head");
        var root = Name(b, "root");
        b.StartElement(root, default);
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.Attribute(Name(b, "id"), "r");
        b.EndAttributes();

        var a = Name(b, "a");
        var bn = Name(b, "b");
        b.StartElement(a, default);
        b.Attribute(Name(b, "n"), "1");
        b.EndAttributes();
        b.Text("t1", XmlTextKind.Text);
        b.StartElement(bn, default);
        b.EndAttributes();
        b.EndElement(bn);
        b.StartElement(bn, default);
        b.Attribute(Name(b, "k"), "x");
        b.EndAttributes();
        b.EndElement(bn);
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        b.EndElement(a);

        b.StartElement(a, default);
        b.Attribute(Name(b, "n"), "2");
        b.EndAttributes();
        b.StartElement(bn, default);
        b.EndAttributes();
        b.EndElement(bn);
        b.EndElement(a);

        var e = Name(b, "e", "p", "urn:p");
        b.StartElement(e, default);
        b.EndAttributes();
        b.EndElement(e);

        b.EndElement(root);
        b.EndDocument();
        return b.Complete();
    }

    private static List<KeyValuePair<string, XPathNode>> Index(XmlDocumentTree tree)
    {
        var doc = tree.Document;
        var head = doc.FirstChild;
        var root = head.NextSibling;
        var a1 = root.FirstChild;
        var b1 = a1.FirstChild.NextSibling;
        var b2 = b1.NextSibling;
        var c = b2.NextSibling;
        var a2 = a1.NextSibling;
        return
        [
            Pair("doc", doc),
            Pair("head", head),
            Pair("root", root),
            Pair("rootId", root.FirstAttribute),
            Pair("a1", a1),
            Pair("a1n", a1.FirstAttribute),
            Pair("t1", a1.FirstChild),
            Pair("b1", b1),
            Pair("b2", b2),
            Pair("b2k", b2.FirstAttribute),
            Pair("c", c),
            Pair("pi", c.NextSibling),
            Pair("a2", a2),
            Pair("a2n", a2.FirstAttribute),
            Pair("a2b", a2.FirstChild),
            Pair("e", a2.NextSibling),
        ];
    }

    private static KeyValuePair<string, XPathNode> Pair(string key, XmlNode node) => new(key, new XPathNode(node));
}
