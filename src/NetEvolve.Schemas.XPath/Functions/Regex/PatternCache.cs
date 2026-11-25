namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// Compiles a pattern and its flags once per pattern string instance. A literal pattern of a compiled expression is one string that
/// every evaluation passes, so it is compiled once per compiled expression; a pattern that an evaluation computes is a new string and
/// is compiled again (decision D2 of #266).
/// </summary>
/// <remarks>
/// <para>
/// The key is the identity of the pattern string, held weakly: the compiled operations live as long as the string does, which for a
/// literal is as long as the compiled expression, so the cache never grows beyond the live expressions and needs no eviction. Under
/// each string the operations are kept per set of flags, so <c>i</c> and <c>ii</c> share one. Thread-safe; two threads that compile
/// the same pattern at once both compile it and one result wins. An invalid pattern is not cached.
/// </para>
/// <para>
/// Limitation: the function call has no per-call-site state (the function conversion and the call node are outside this package of
/// work), so the cache cannot tell a literal from a computed string; a computed pattern that is the same string instance, such as a
/// variable, is cached as well. The name characters of <c>\i</c> and <c>\c</c> are those of XML 1.0 Second Edition, the edition of
/// XSD 1.0, which Functions and Operators 1.0 cites (decision D3 of #266).
/// </para>
/// </remarks>
internal sealed class PatternCache
{
    // One slot per set of flags: the four flags of RegexOptions give the values 0 to 15.
    private const int Slots = 16;

    private readonly ConditionalWeakTable<string, RegexOperations?[]> _entries = new();
    private readonly ConditionalWeakTable<RegexOperations, int[]> _parents = new();
    private int _compilations;

    /// <summary>Gets the cache of the built-in functions, shared by <c>fn:matches</c>, <c>fn:replace</c>, and <c>fn:tokenize</c>.</summary>
    internal static PatternCache Shared { get; } = new();

    /// <summary>Gets the number of compilations so far; a test observes that a literal pattern is compiled once.</summary>
    internal int Compilations => Volatile.Read(ref _compilations);

    /// <summary>Gets the operations of a pattern, compiling them when this string instance has no operations for these flags yet.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="flags">The flag letters of the call, the empty string for none.</param>
    /// <returns>The operations.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">A flag is invalid (FORX0001), the pattern is invalid (FORX0002), or it exceeds a limit of the regex engine.</exception>
    internal RegexOperations Get(string pattern, string flags)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(flags);

        var error = RegexOptionsParser.TryParse(flags.AsSpan(), out var options, out _);
        if (error != RegexError.None)
        {
            throw RegexErrors.Pattern(error);
        }

        var slots = _entries.TryGetValue(pattern, out var found)
            ? found
            : _entries.GetValue(pattern, static _ => new RegexOperations?[Slots]);
        var slot = (int)options;
        var operations = Volatile.Read(ref slots[slot]);
        if (operations is not null)
        {
            return operations;
        }

        operations = Compile(pattern, options);
        _ = Interlocked.Increment(ref _compilations);
        return Interlocked.CompareExchange(ref slots[slot], operations, null) ?? operations;
    }

    /// <summary>Gets the enclosing capture group of each capture group of a pattern that this cache compiled.</summary>
    /// <param name="operations">The operations, as <see cref="Get"/> returned them.</param>
    /// <returns>An array indexed by group number whose entry is the number of the innermost capture group around that group, zero for none.</returns>
    internal int[] GroupParents(RegexOperations operations)
    {
        // Every operations instance Get returns is registered by Compile, so the lookup cannot miss.
        _ = _parents.TryGetValue(operations, out var parents);
        return parents!;
    }

    private RegexOperations Compile(string pattern, RegexOptions options)
    {
        var parsed = RegexParser.Parse(
            pattern.AsSpan(),
            new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10SecondEdition, options)
        );
        if (!parsed.Succeeded)
        {
            throw RegexErrors.Pattern(parsed.Error);
        }

        var created = RegexOperations.TryCreate(parsed.Tree!, out var operations);
        if (created != RegexError.None)
        {
            throw RegexErrors.Pattern(created);
        }

        var parents = Parents(parsed.Tree!, operations!.GroupCount);
        _ = _parents.GetValue(operations, _ => parents);
        return operations;
    }

    private static int[] Parents(RegexTree tree, int groupCount)
    {
        var parents = new int[groupCount + 1];
        var nodes = tree.Nodes;
        var pending = new Stack<(int Node, int Parent)>();
        pending.Push((tree.Root, 0));
        while (pending.Count > 0)
        {
            var (index, parent) = pending.Pop();
            for (; index != RegexNode.NoNode; index = nodes[index].NextSibling)
            {
                var node = nodes[index];
                var inner = parent;
                if (node.Kind == RegexNodeKind.Group && node.Value != RegexNode.NoGroup)
                {
                    parents[node.Value] = parent;
                    inner = node.Value;
                }

                if (node.FirstChild != RegexNode.NoNode)
                {
                    pending.Push((node.FirstChild, inner));
                }
            }
        }

        return parents;
    }
}
