namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

/// <summary>Counts the XSLT constructs a rule set embeds.</summary>
/// <remarks>
/// Limitations: function names are found by a lexical scan of expression attributes, so the counts are
/// approximate (attribute value templates and names produced at run time are not seen). XInclude
/// <c>xpointer</c> is not evaluated and <c>parse="text"</c> parts are skipped; a part is scanned as a whole unless
/// a Schematron include names a fragment. Only relative references that stay inside the suite root are followed,
/// and every part is counted once per rule set.
/// </remarks>
internal static partial class CorpusScanner
{
    private const int MatchTimeoutMilliseconds = 5000;
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";
    private const string XIncludeNamespace = "http://www.w3.org/2001/XInclude";
    private const string SchematronNamespace = "http://purl.oclc.org/dsdl/schematron";

    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
    };

    private static readonly HashSet<string> ExpressionAttributes = new(StringComparer.Ordinal)
    {
        "select",
        "test",
        "match",
        "use",
        "context",
        "value",
        "subject",
        "path",
        "count",
        "from",
        "group-by",
        "group-adjacent",
        "group-starting-with",
        "group-ending-with",
    };

    // Names followed by a parenthesis that are keywords, operators, or node tests, not function calls.
    private static readonly HashSet<string> NonFunctionNames = new(StringComparer.Ordinal)
    {
        "if",
        "for",
        "some",
        "every",
        "then",
        "else",
        "return",
        "satisfies",
        "in",
        "and",
        "or",
        "div",
        "idiv",
        "mod",
        "to",
        "union",
        "intersect",
        "except",
        "is",
        "eq",
        "ne",
        "lt",
        "le",
        "gt",
        "ge",
        "instance",
        "of",
        "treat",
        "as",
        "cast",
        "castable",
        "typeswitch",
        "element",
        "attribute",
        "text",
        "node",
        "comment",
        "document-node",
        "processing-instruction",
        "namespace-node",
        "schema-element",
        "schema-attribute",
        "item",
        "empty-sequence",
    };

    /// <summary>Scans a rule set and the parts it includes.</summary>
    /// <param name="ruleSetPath">The rule set file.</param>
    /// <param name="root">The directory that includes must stay inside.</param>
    /// <param name="name">The display name of the rule set.</param>
    /// <returns>The scan result.</returns>
    public static RuleSetScan Scan(string ruleSetPath, string root, string name)
    {
        var state = new ScanState(Path.GetFullPath(root));
        Visit(Path.GetFullPath(ruleSetPath), null, ruleSetPath, state);
        return new RuleSetScan(name, state.Elements, state.Functions, state.Unresolved);
    }

    /// <summary>Tells whether a file is a Schematron schema document.</summary>
    /// <param name="path">The file to inspect.</param>
    /// <returns><see langword="true"/> when the root element is <c>schema</c> in the Schematron namespace.</returns>
    public static bool IsRuleSet(string path)
    {
        try
        {
            using var reader = XmlReader.Create(path, ReaderSettings);
            return reader.MoveToContent() == XmlNodeType.Element
                && string.Equals(reader.NamespaceURI, SchematronNamespace, StringComparison.Ordinal)
                && string.Equals(reader.LocalName, "schema", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void Visit(string path, string? fragment, string display, ScanState state)
    {
        if (!state.Visited.Add(fragment is null ? path : path + "#" + fragment))
        {
            return;
        }

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(path, ReaderSettings);
            document = XDocument.Load(reader);
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            state.Unresolved.Add($"{display}: {ex.GetType().Name}");
            return;
        }

        var start = fragment is null ? document.Root : FindById(document.Root, fragment);
        if (start is null)
        {
            if (fragment is not null)
            {
                state.Unresolved.Add($"{display}: fragment '{fragment}' not found");
            }
            return;
        }

        Walk(start, path, state);
    }

    private static void Walk(XElement element, string path, ScanState state)
    {
        var ns = element.Name.NamespaceName;
        var local = element.Name.LocalName;

        var xinclude = string.Equals(ns, XIncludeNamespace, StringComparison.Ordinal);
        if (
            string.Equals(local, "include", StringComparison.Ordinal)
            && (xinclude || string.Equals(ns, SchematronNamespace, StringComparison.Ordinal))
        )
        {
            Follow(element, xinclude, path, state);
            return;
        }

        if (string.Equals(ns, XsltNamespace, StringComparison.Ordinal))
        {
            Increment(state.Elements, "xsl:" + local);
        }

        if (
            string.Equals(ns, XsltNamespace, StringComparison.Ordinal)
            || string.Equals(ns, SchematronNamespace, StringComparison.Ordinal)
        )
        {
            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.NamespaceName.Length == 0 && ExpressionAttributes.Contains(attribute.Name.LocalName))
                {
                    CountFunctions(attribute.Value, state.Functions);
                }
            }
        }

        foreach (var child in element.Elements())
        {
            Walk(child, path, state);
        }
    }

    private static void Follow(XElement include, bool xinclude, string path, ScanState state)
    {
        if (xinclude && string.Equals((string?)include.Attribute("parse"), "text", StringComparison.Ordinal))
        {
            return;
        }

        var href = (string?)include.Attribute("href");
        if (string.IsNullOrEmpty(href))
        {
            state.Unresolved.Add("include without href: nothing to follow");
            return;
        }

        if (!Uri.TryCreate(href, UriKind.Relative, out _))
        {
            state.Unresolved.Add($"{href}: not a relative reference");
            return;
        }

        var hash = href.IndexOf('#', StringComparison.Ordinal);
        var file = hash < 0 ? href : href[..hash];
        var fragment = hash < 0 || hash == href.Length - 1 ? null : Uri.UnescapeDataString(href[(hash + 1)..]);
        var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, Uri.UnescapeDataString(file)));
        if (!target.StartsWith(state.Root, StringComparison.Ordinal))
        {
            state.Unresolved.Add($"{href}: outside the suite root");
            return;
        }

        if (!File.Exists(target))
        {
            state.Unresolved.Add($"{href}: file not found");
            return;
        }

        Visit(target, fragment, href, state);
    }

    private static XElement? FindById(XElement? root, string id)
    {
        if (root is null)
        {
            return null;
        }

        foreach (var element in root.DescendantsAndSelf())
        {
            if (
                string.Equals((string?)element.Attribute("id"), id, StringComparison.Ordinal)
                || string.Equals((string?)element.Attribute(XNamespace.Xml + "id"), id, StringComparison.Ordinal)
            )
            {
                return element;
            }
        }
        return null;
    }

    private static void CountFunctions(string expression, SortedDictionary<string, int> functions)
    {
        var stripped = XPathComment().Replace(StringLiteral().Replace(expression, " "), " ");
        foreach (Match match in FunctionCall().Matches(stripped))
        {
            var name = match.Groups["name"].Value;
            if (!NonFunctionNames.Contains(name))
            {
                Increment(functions, name);
            }
        }
    }

    private static void Increment(SortedDictionary<string, int> counts, string key) =>
        counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;

    [GeneratedRegex(@"'[^']*'|""[^""]*""", RegexOptions.CultureInvariant, MatchTimeoutMilliseconds)]
    private static partial Regex StringLiteral();

    [GeneratedRegex(@"\(:.*?:\)", RegexOptions.CultureInvariant | RegexOptions.Singleline, MatchTimeoutMilliseconds)]
    private static partial Regex XPathComment();

    [GeneratedRegex(
        @"(?<![\w.\-:$@])(?<name>(?:[A-Za-z_][\w.\-]*:)?[A-Za-z_][\w.\-]*)\s*\(",
        RegexOptions.CultureInvariant,
        MatchTimeoutMilliseconds
    )]
    private static partial Regex FunctionCall();

    private sealed class ScanState(string root)
    {
        public string Root { get; } = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;

        public HashSet<string> Visited { get; } = new(StringComparer.Ordinal);

        public SortedDictionary<string, int> Elements { get; } = new(StringComparer.Ordinal);

        public SortedDictionary<string, int> Functions { get; } = new(StringComparer.Ordinal);

        public List<string> Unresolved { get; } = [];
    }
}
