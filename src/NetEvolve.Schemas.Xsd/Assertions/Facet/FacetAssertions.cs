namespace NetEvolve.Schemas.Xsd.Assertions.Facet;

using System.Collections.Concurrent;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// The assertion facets of the simple types of a schema, with the ones a type inherits from its base types (Datatypes section 4.3.13: the
/// facet of a restriction adds to those of its base type).
/// </summary>
/// <remarks>
/// The assertions of a type come after those of its base types, so the order of the issues is the order of the derivation chain and then
/// document order. The component model does not hold the assertions of a type (the wiring is XSD-78), so the table is created from the
/// compiled <c>assertion</c> elements. Immutable apart from the cache of the flattened lists, so one instance serves any number of threads.
/// </remarks>
internal sealed class FacetAssertions
{
    private static readonly FacetAssertion[] None = [];

    private readonly IReadOnlyDictionary<SimpleTypeDefinition, IReadOnlyList<FacetAssertion>> _own;
    private readonly ConcurrentDictionary<SimpleTypeDefinition, IReadOnlyList<FacetAssertion>> _flattened = new();

    /// <summary>Initializes a new instance of the <see cref="FacetAssertions"/> class.</summary>
    /// <param name="own">The assertions that each simple type declares itself, in document order.</param>
    internal FacetAssertions(IReadOnlyDictionary<SimpleTypeDefinition, IReadOnlyList<FacetAssertion>> own)
    {
        Argument.ThrowIfNull(own);

        _own = own;
    }

    /// <summary>Gets the assertions of a type: those of its base types first, then its own.</summary>
    /// <param name="type">The simple type.</param>
    /// <returns>The assertions, empty for none. The list is the same instance on every call.</returns>
    internal IReadOnlyList<FacetAssertion> Of(SimpleTypeDefinition type)
    {
        Argument.ThrowIfNull(type);

        if (_own.Count == 0)
        {
            return None;
        }

        if (_flattened.TryGetValue(type, out var cached))
        {
            return cached;
        }

        return _flattened.GetOrAdd(type, Flatten(type));
    }

    private FacetAssertion[] Flatten(SimpleTypeDefinition type)
    {
        // A repeated type ends the chain, so a defective chain cannot loop.
        var chain = new List<SimpleTypeDefinition>(4);
        var current = type;
        while (current is not null && !chain.Contains(current))
        {
            chain.Add(current);
            current = current.BaseType;
        }

        var all = new List<FacetAssertion>();
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            if (_own.TryGetValue(chain[i], out var own))
            {
                all.AddRange(own);
            }
        }

        return all.Count == 0 ? None : all.ToArray();
    }
}
