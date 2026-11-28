namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using System.Collections.Concurrent;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The assertions of the complex types of a schema, with the ones a type inherits from its base types (Structures section 3.4.4: an
/// element is valid against a type only if it satisfies the assertions of the type and of every complex type it derives from).
/// </summary>
/// <remarks>
/// The assertions of a type come after those of its base types, so the order of the issues is the order of the derivation chain and then
/// document order. The component model does not hold the assertions of a type yet (the wiring is XSD-78), so the table is created from the
/// compiled <c>assert</c> elements. Immutable apart from the cache of the flattened lists, so one instance serves any number of threads.
/// </remarks>
internal sealed class ComplexTypeAssertions
{
    private static readonly ComplexTypeAssertion[] None = [];

    private readonly IReadOnlyDictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> _own;
    private readonly ConcurrentDictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> _flattened =
        new();

    /// <summary>Initializes a new instance of the <see cref="ComplexTypeAssertions"/> class.</summary>
    /// <param name="own">The assertions that each complex type declares itself, in document order.</param>
    internal ComplexTypeAssertions(IReadOnlyDictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> own)
    {
        ArgumentNullException.ThrowIfNull(own);

        _own = own;
    }

    /// <summary>Gets the number of complex types that declare assertions.</summary>
    internal int Count => _own.Count;

    /// <summary>Gets the assertions of a type: those of its base types first, then its own.</summary>
    /// <param name="type">The type of an element.</param>
    /// <returns>The assertions, empty for none and for a type that is no complex type. The list is the same instance on every call.</returns>
    internal IReadOnlyList<ComplexTypeAssertion> Of(IXsdComponent? type)
    {
        if (type is not ComplexTypeDefinition complex || _own.Count == 0)
        {
            return None;
        }

        if (_flattened.TryGetValue(complex, out var cached))
        {
            return cached;
        }

        return _flattened.GetOrAdd(complex, Flatten(complex));
    }

    private ComplexTypeAssertion[] Flatten(ComplexTypeDefinition type)
    {
        // The chain ends at xs:anyType, which is its own base; a repeated type ends it as well, so a defective chain cannot loop.
        var chain = new List<ComplexTypeDefinition>(4);
        var current = type;
        while (current is not null && !chain.Contains(current))
        {
            chain.Add(current);
            current = current.BaseType as ComplexTypeDefinition;
        }

        var all = new List<ComplexTypeAssertion>();
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
