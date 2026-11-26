namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The key indexes of one run (XSLT 2.0 section 16.3). An index is built lazily, on the first lookup of a key name in a document, and
/// once per document and name: it walks the document once, matches every node against every declaration of the name through the
/// shared pattern matcher, evaluates the key value of the matching nodes with the node as the focus, and keeps the nodes by key value
/// in document order without duplicates. A document is the tree of a document node; a lookup in another tree is XTDE1270. Not
/// thread-safe: one provider per run, as the run is.
/// </summary>
/// <remarks>
/// The function <c>key</c> of the XPath package reaches the provider through <see cref="IXPathKeyProvider"/>, which names the
/// context node or the top node (#776, #777); a problem of that lookup is an <see cref="XPathException"/>. The index holds every matching node, so its size is proportional to the matches, as the key tables of
/// identity constraints are (design #146). A match pattern may start with <c>key()</c> (#780); a key whose pattern reads the key
/// under construction is the circularity error XTDE0640.
/// </remarks>
internal sealed class XsltKeyProvider : IXPathKeyProvider
{
    private readonly XsltKeyTable _table;
    private readonly IPatternMatcher _matcher;
    private readonly IQueryRun _run;
    private readonly ISlotBinder _binder;
    private readonly XmlNameTable _names;
    private readonly CancellationToken _cancellationToken;
    private readonly XsltFrame _frame;
    private readonly Dictionary<(XPathNode Root, XPathQName Name), Dictionary<string, List<XPathNode>>> _indexes = [];
    private readonly HashSet<(XPathNode Root, XPathQName Name)> _building = [];
    private SchematronProblem _problem;

    /// <summary>Initializes a new instance of the <see cref="XsltKeyProvider"/> class.</summary>
    /// <param name="table">The key declarations of the schema.</param>
    /// <param name="matcher">The shared pattern matcher that matches the nodes against the declarations.</param>
    /// <param name="run">The run that evaluates the key values.</param>
    /// <param name="binder">The binder of the schema-level slots that a declaration reads.</param>
    /// <param name="names">The name table of the run, for the sequences that a key value builds.</param>
    /// <param name="cancellationToken">The token checked for every node of an index build.</param>
    /// <exception cref="System.ArgumentNullException">An argument other than the token is <see langword="null"/>.</exception>
    internal XsltKeyProvider(
        XsltKeyTable table,
        IPatternMatcher matcher,
        IQueryRun run,
        ISlotBinder binder,
        XmlNameTable names,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(names);

        _table = table;
        _matcher = matcher;
        _run = run;
        _binder = binder;
        _names = names;
        _cancellationToken = cancellationToken;
        _frame = new XsltFrame(run, cancellationToken);
    }

    /// <summary>Gets the number of indexes built so far: one per key name and document that was looked up.</summary>
    internal int IndexBuilds { get; private set; }

    /// <summary>Gets the problem of the last lookup that returned <see cref="QueryStatus.Problem"/> or <see langword="false"/>.</summary>
    /// <returns>The problem.</returns>
    internal SchematronProblem GetProblem() => _problem;

    /// <inheritdoc />
    /// <exception cref="XPathException">The lookup is a problem (<see cref="TryLookup(in XPathQName, in XdmSequence, in XPathNode, bool, out XdmSequence)"/>); the code is the constraint of the problem, such as XTDE1260 or XTDE1270.</exception>
    bool IXPathKeyProvider.TryLookup(
        in XPathQName name,
        in XdmSequence values,
        in XPathNode node,
        bool subtree,
        out XdmSequence nodes
    )
    {
        if (TryLookup(in name, in values, in node, subtree, out nodes) == QueryStatus.Ok)
        {
            return true;
        }

        var problem = _problem;
        throw problem.Constraint is null
            ? new XPathException(problem.Message)
            : new XPathException(
                new XPathDiagnostic(
                    new XPathErrorCode(problem.Constraint),
                    XPathErrorKind.Dynamic,
                    problem.Message,
                    -1,
                    0
                )
            );
    }

    /// <summary>
    /// Finds the nodes of a key: <c>key(name, values)</c> with the context node, and <c>key(name, values, top)</c> with
    /// <paramref name="subtree"/>. The result is the union of the nodes of every value, in document order, without duplicates.
    /// </summary>
    /// <param name="name">The expanded key name.</param>
    /// <param name="values">The values to look up; each is atomized.</param>
    /// <param name="node">The context node, whose document is searched, or the top node of the three-argument form.</param>
    /// <param name="subtree"><see langword="true"/> to keep only the nodes that are <paramref name="node"/> or its descendants.</param>
    /// <param name="nodes">The nodes when the result is <see cref="QueryStatus.Ok"/>.</param>
    /// <returns><see cref="QueryStatus.Problem"/> for an undeclared key (XTDE1260), a node outside a document (XTDE1270), a key value of an unsupported type, or a failure of the index build; read the problem with <see cref="GetProblem"/>.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="node"/> is no node.</exception>
    /// <exception cref="System.OperationCanceledException">The token of the provider is canceled during an index build.</exception>
    internal QueryStatus TryLookup(
        in XPathQName name,
        in XdmSequence values,
        in XPathNode node,
        bool subtree,
        out XdmSequence nodes
    )
    {
        ArgumentNullException.ThrowIfNull(node.Tree);

        nodes = XdmSequence.Empty;
        if (!_table.TryGet(in name, out var declarations))
        {
            return Fail("XTDE1260", $"The key '{name}' is not declared by an xsl:key element.");
        }

        var root = node;
        for (var parent = root.Parent(); !parent.IsEmpty(); parent = root.Parent())
        {
            root = parent;
        }

        if (root.Kind != XdmNodeKind.Document)
        {
            return Fail(
                "XTDE1270",
                "The root of the tree of the node is not a document node, so no key index exists for it."
            );
        }

        if (!_indexes.TryGetValue((root, name), out var index))
        {
            // A match pattern that starts with key() of the key under construction would recurse without end (XTDE0640).
            if (!_building.Add((root, name)))
            {
                return Fail(
                    "XTDE0640",
                    $"The definition of the key '{name}' is circular: its match pattern reads the key."
                );
            }

            var built = TryBuild(in root, declarations, out index);
            _ = _building.Remove((root, name));
            if (built == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            _indexes.Add((root, name), index);
            IndexBuilds++;
        }

        var found = new List<XPathNode>();
        for (var i = 0; i < values.Count; i++)
        {
            var status = XsltKeyValue.TryGetKey(values[i], out var key);
            if (status == XsltKeyValueStatus.Unsupported)
            {
                return Fail(null, $"The type of a value of the key '{name}' is not supported as a key value.");
            }

            if (status == XsltKeyValueStatus.Ok && index.TryGetValue(key, out var bucket))
            {
                AddNodes(found, bucket, in node, subtree);
            }
        }

        nodes = ToSequence(found, values.Count > 1);
        return QueryStatus.Ok;
    }

    private static void AddNodes(List<XPathNode> found, List<XPathNode> bucket, in XPathNode top, bool subtree)
    {
        for (var i = 0; i < bucket.Count; i++)
        {
            if (!subtree || IsSelfOrDescendant(bucket[i], in top))
            {
                found.Add(bucket[i]);
            }
        }
    }

    private static bool IsSelfOrDescendant(XPathNode candidate, in XPathNode top)
    {
        for (var node = candidate; !node.IsEmpty(); node = node.Parent())
        {
            if (node == top)
            {
                return true;
            }
        }

        return false;
    }

    // One value gives the bucket, which is in document order without duplicates; several values need the union.
    private static XdmSequence ToSequence(List<XPathNode> found, bool union)
    {
        if (union)
        {
            found.Sort(static (left, right) => left.CompareDocumentOrder(right));
        }

        var items = new List<XdmItem>(found.Count);
        for (var i = 0; i < found.Count; i++)
        {
            if (!union || i == 0 || found[i] != found[i - 1])
            {
                items.Add(XdmItem.FromNode(found[i]));
            }
        }

        return XdmSequence.Create(items.ToArray());
    }

    private QueryStatus TryBuild(
        in XPathNode root,
        XsltKeyComponent[] declarations,
        out Dictionary<string, List<XPathNode>> index
    )
    {
        index = new Dictionary<string, List<XPathNode>>(StringComparer.Ordinal);
        foreach (var declaration in declarations)
        {
            if (_binder.EnsureBound(declaration.ReferencedSlots) == QueryStatus.Problem)
            {
                _problem = _frame.TakeProblem();
                return QueryStatus.Problem;
            }
        }

        // Document order: a node, its attributes, then its children. The walk climbs by the parent, so a deep document needs no stack.
        var node = root;
        while (true)
        {
            if (TryIndexNode(declarations, in node, index) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }

            if (node.Kind == XdmNodeKind.Element)
            {
                for (var attribute = node.FirstAttribute(); !attribute.IsEmpty(); attribute = attribute.NextAttribute())
                {
                    if (TryIndexNode(declarations, in attribute, index) == QueryStatus.Problem)
                    {
                        return QueryStatus.Problem;
                    }
                }
            }

            var next = node.FirstChild();
            while (next.IsEmpty())
            {
                if (node == root)
                {
                    return QueryStatus.Ok;
                }

                next = node.NextSibling();
                if (next.IsEmpty())
                {
                    node = node.Parent();
                }
            }

            node = next;
        }
    }

    private QueryStatus TryIndexNode(
        XsltKeyComponent[] declarations,
        in XPathNode node,
        Dictionary<string, List<XPathNode>> index
    )
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var kind = Kinds(node.Kind);
        foreach (var declaration in declarations)
        {
            // The kinds of the pattern are a cheap pre-test; most nodes of a document are not candidates.
            if ((declaration.Match.NodeKinds & kind) == QueryNodeKinds.None)
            {
                continue;
            }

            if (!TryMatches(declaration, in node, out var matched))
            {
                return QueryStatus.Problem;
            }

            if (matched && TryAddKeys(declaration, in node, index) == QueryStatus.Problem)
            {
                return QueryStatus.Problem;
            }
        }

        return QueryStatus.Ok;
    }

    private bool TryMatches(XsltKeyComponent declaration, in XPathNode node, out bool matched)
    {
        if (_matcher.Matches(declaration.Match, _run, in node, out matched) == QueryStatus.Ok)
        {
            return true;
        }

        _problem = _run.GetProblem();
        return false;
    }

    // The key value of a matching node is evaluated with the node as the context item, position 1 of 1 (XSLT 2.0 section 16.3).
    private QueryStatus TryAddKeys(
        XsltKeyComponent declaration,
        in XPathNode node,
        Dictionary<string, List<XPathNode>> index
    )
    {
        var sink = new ItemSequenceSink(_names);
        var focus = new QueryFocus(node, 1, 1);
        QueryStatus status;
        if (declaration.Use is not null)
        {
            status = _run.TryEvaluate(declaration.Use, in focus, sink);
        }
        else
        {
            _frame.Focus = focus;
            status = declaration.Body!.TryExecute(_frame, sink);
        }

        if (status == QueryStatus.Problem)
        {
            _problem = _frame.TakeProblem();
            return QueryStatus.Problem;
        }

        var values = sink.Result;
        for (var i = 0; i < values.Count; i++)
        {
            var result = XsltKeyValue.TryGetKey(values[i], out var key);
            if (result == XsltKeyValueStatus.Unsupported)
            {
                return Fail(
                    null,
                    $"The key value of the key '{declaration.DisplayName}' has a type that is not supported as a key value."
                );
            }

            if (result == XsltKeyValueStatus.Ok)
            {
                Add(index, key, node);
            }
        }

        return QueryStatus.Ok;
    }

    // The nodes arrive in document order, so a node that was added last for a key is the only duplicate that can occur.
    private static void Add(Dictionary<string, List<XPathNode>> index, string key, XPathNode node)
    {
        if (!index.TryGetValue(key, out var bucket))
        {
            bucket = [];
            index.Add(key, bucket);
        }

        if (bucket.Count == 0 || bucket[bucket.Count - 1] != node)
        {
            bucket.Add(node);
        }
    }

    // The flags of QueryNodeKinds are the numbers of XdmNodeKind as bit positions: document 1, element 2, attribute 4, and so on.
    private static QueryNodeKinds Kinds(XdmNodeKind kind) => (QueryNodeKinds)(1 << (int)kind);

    private QueryStatus Fail(string? constraint, string message)
    {
        _problem = Problem(constraint, message);
        return QueryStatus.Problem;
    }

    private static SchematronProblem Problem(string? constraint, string message) =>
        new(SchematronDiagnosticCodes.EvaluationFailed, message, default, constraint);
}
