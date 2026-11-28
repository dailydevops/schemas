namespace NetEvolve.Schemas.Schematron.Evaluation.Dispatch;

using System;
using System.Buffers;
using System.Threading;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The result of the one traversal that serves all active patterns (SCH-72): every node that at least one rule context can match,
/// in document order, with the patterns that may match it (see <see cref="DispatchTable"/>). A node outside the index cannot be
/// matched by any rule, so skipping it changes neither the fired rules nor their order, because every pattern still visits its
/// candidates in document order.
/// </summary>
/// <remarks>
/// Not thread-safe: one index per run. The per-node cost is an array read; the entries live in pooled arrays that
/// <see cref="Dispose"/> returns, so a run allocates only the small index object.
/// </remarks>
internal sealed class DispatchIndex : IDisposable
{
    private const int CancellationMask = 255;
    private const int InitialCapacity = 256;

    // The patterns of a node name that was resolved and has none; a cache entry that was never set is null.
    private static readonly bool[] NoPatterns = [];

    private readonly XmlTree _tree;
    private int[] _nodes = ArrayPool<int>.Shared.Rent(InitialCapacity);
    private bool[]?[] _candidates = ArrayPool<bool[]?>.Shared.Rent(InitialCapacity);

    private DispatchIndex(XmlTree tree) => _tree = tree;

    /// <summary>Gets the number of candidate nodes.</summary>
    internal int Count { get; private set; }

    /// <summary>Determines whether a pattern may match a candidate node.</summary>
    /// <param name="entry">The index of the candidate, below <see cref="Count"/>.</param>
    /// <param name="set">The index of the pattern.</param>
    /// <returns><see langword="true"/> when a rule of the pattern can match the node kind and name.</returns>
    internal bool Includes(int entry, int set) => _candidates[entry]![set];

    /// <summary>Gets a candidate node.</summary>
    /// <param name="entry">The index of the candidate, below <see cref="Count"/>.</param>
    /// <returns>The node.</returns>
    internal XmlNode NodeAt(int entry) => new(_tree, _nodes[entry]);

    /// <inheritdoc />
    public void Dispose()
    {
        ArrayPool<int>.Shared.Return(_nodes);
        ArrayPool<bool[]?>.Shared.Return(_candidates, clearArray: true);
        _nodes = [];
        _candidates = [];
        Count = 0;
    }

    /// <summary>Builds the index with one traversal of the instance.</summary>
    /// <param name="table">The rule index of the active patterns.</param>
    /// <param name="root">The document node of the instance.</param>
    /// <param name="cancellationToken">Checked every 256th visited node.</param>
    /// <returns>The index; the caller disposes it.</returns>
    /// <exception cref="OperationCanceledException">The token was canceled.</exception>
    internal static DispatchIndex Build(DispatchTable table, XmlNode root, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(root.Tree);

        var index = new DispatchIndex(root.Tree);
        NameCache?[]? caches = null;
        try
        {
            var visited = 0;
            for (var node = root; !node.IsEmpty; node = PatternEvaluator.Next(node))
            {
                if ((++visited & CancellationMask) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var position = DispatchTable.PositionOf(PatternEvaluator.KindOf(node.Kind));
                if (position < 0)
                {
                    continue;
                }

                var candidates = table.HasNames(position)
                    ? index.Resolve(table, ref caches, position, node)
                    : table.Candidates(position, default);
                if (candidates is { Length: > 0 })
                {
                    index.Add(node.Index, candidates);
                }
            }

            return index;
        }
        catch
        {
            index.Dispose();
            throw;
        }
        finally
        {
            if (caches is not null)
            {
                foreach (var cache in caches)
                {
                    cache?.Release();
                }
            }
        }
    }

    // The patterns of a node by its expanded name, remembered per local name symbol so that the strings are read once per name.
    private bool[]? Resolve(DispatchTable table, ref NameCache?[]? caches, int position, XmlNode node)
    {
        var name = node.Name;
        var local = name.Local.Value;
        var names = _tree.Names;
        caches ??= new NameCache?[DispatchTable.KindCount];
        var cache = caches[position] ??= new NameCache(names.Count);
        if ((uint)local < (uint)cache.Candidates.Length)
        {
            var cached = cache.Candidates[local];
            if (cached is not null && cache.Namespaces[local] == name.NamespaceUri.Value)
            {
                return cached;
            }
        }

        var resolved = table.Candidates(
            position,
            new XPathQName(Text(names, name.NamespaceUri), Text(names, name.Local))
        ) ?? NoPatterns;
        if ((uint)local < (uint)cache.Candidates.Length)
        {
            cache.Candidates[local] = resolved;
            cache.Namespaces[local] = name.NamespaceUri.Value;
        }

        return resolved;
    }

    private void Add(int node, bool[] candidates)
    {
        if (Count == _nodes.Length)
        {
            var size = Count * 2;
            var nodes = ArrayPool<int>.Shared.Rent(size);
            var flags = ArrayPool<bool[]?>.Shared.Rent(size);
            Array.Copy(_nodes, nodes, Count);
            Array.Copy(_candidates, flags, Count);
            ArrayPool<int>.Shared.Return(_nodes);
            ArrayPool<bool[]?>.Shared.Return(_candidates, clearArray: true);
            _nodes = nodes;
            _candidates = flags;
        }

        _nodes[Count] = node;
        _candidates[Count] = candidates;
        Count++;
    }

    private static string Text(XmlNameTable names, XmlSymbol symbol) =>
        names.TryGetString(symbol, out var text) && text is not null ? text : string.Empty;

    // Per node kind: the resolved patterns by local name symbol, and the namespace symbol they were resolved for. A second namespace
    // for the same local name is resolved again every time, which is rare.
    private sealed class NameCache
    {
        internal NameCache(int size)
        {
            Candidates = ArrayPool<bool[]?>.Shared.Rent(size);
            Namespaces = ArrayPool<int>.Shared.Rent(size);
        }

        internal bool[]?[] Candidates { get; }

        internal int[] Namespaces { get; }

        internal void Release()
        {
            ArrayPool<bool[]?>.Shared.Return(Candidates, clearArray: true);
            ArrayPool<int>.Shared.Return(Namespaces);
        }
    }
}
