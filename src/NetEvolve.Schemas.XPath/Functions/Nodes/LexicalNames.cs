namespace NetEvolve.Schemas.XPath.Functions.Nodes;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The lexical form (<c>prefix:local</c>) of node names, built once per pair of name table symbols. A name table is shared by every
/// document of a validator, so a rule that reads <c>fn:name</c> on every node does not allocate a string per call.
/// </summary>
/// <remarks>The cache lives as long as its name table and holds one string per distinct prefixed name that was read; it is safe for concurrent readers.</remarks>
internal static class LexicalNames
{
    private static readonly ConditionalWeakTable<XmlNameTable, ConcurrentDictionary<long, string>> Cache = new();

    /// <summary>Gets the lexical name.</summary>
    /// <param name="names">The name table of the tree of the node.</param>
    /// <param name="name">The name of the node.</param>
    /// <returns>The local name when there is no prefix, otherwise <c>prefix:local</c>.</returns>
    internal static string Get(XmlNameTable names, in XmlName name)
    {
        var local = names.GetString(name.Local);
        if (name.Prefix.Value == 0)
        {
            return local;
        }

        var key = ((long)name.Prefix.Value << 32) | (uint)name.Local.Value;
        var cache = Cache.GetValue(names, static _ => new ConcurrentDictionary<long, string>());
        if (!cache.TryGetValue(key, out var text))
        {
            text = cache.GetOrAdd(key, names.GetString(name.Prefix) + ":" + local);
        }

        return text;
    }
}
