namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The key declarations of a compiled schema by expanded name. Several declarations of one name are all kept, in declaration order:
/// their matches add up (XSLT 2.0 section 16.3) and no import precedence removes one. Immutable and thread-safe.
/// </summary>
internal sealed class XsltKeyTable
{
    private readonly Dictionary<XPathQName, XsltKeyComponent[]> _byName;

    private XsltKeyTable(Dictionary<XPathQName, XsltKeyComponent[]> byName) => _byName = byName;

    /// <summary>Gets the number of distinct key names.</summary>
    internal int Count => _byName.Count;

    /// <summary>Creates a table from the declarations; the order of the declarations of one name is kept.</summary>
    /// <param name="declarations">The declarations in declaration order.</param>
    /// <returns>The table.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="declarations"/> is <see langword="null"/>.</exception>
    internal static XsltKeyTable Create(IReadOnlyList<XsltKeyComponent> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);

        var lists = new Dictionary<XPathQName, List<XsltKeyComponent>>();
        for (var i = 0; i < declarations.Count; i++)
        {
            var declaration = declarations[i];
            if (!lists.TryGetValue(declaration.Name, out var list))
            {
                list = [];
                lists.Add(declaration.Name, list);
            }

            list.Add(declaration);
        }

        var byName = new Dictionary<XPathQName, XsltKeyComponent[]>(lists.Count);
        foreach (var pair in lists)
        {
            byName.Add(pair.Key, pair.Value.ToArray());
        }

        return new XsltKeyTable(byName);
    }

    /// <summary>Finds the declarations of a key name.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="declarations">The declarations in declaration order when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when at least one declaration has the name.</returns>
    internal bool TryGet(in XPathQName name, out XsltKeyComponent[] declarations)
    {
        if (_byName.TryGetValue(name, out var found))
        {
            declarations = found;
            return true;
        }

        declarations = [];
        return false;
    }
}
