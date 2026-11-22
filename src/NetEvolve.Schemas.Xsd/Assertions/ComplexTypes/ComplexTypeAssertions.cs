namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The assertions of the complex types of a schema, with the ones a type inherits from its base types.</summary>
internal sealed class ComplexTypeAssertions
{
    private readonly IReadOnlyDictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> _own;

    /// <summary>Initializes a new instance of the <see cref="ComplexTypeAssertions"/> class.</summary>
    /// <param name="own">The assertions that each complex type declares itself, in document order.</param>
    internal ComplexTypeAssertions(IReadOnlyDictionary<ComplexTypeDefinition, IReadOnlyList<ComplexTypeAssertion>> own)
    {
        Argument.ThrowIfNull(own);

        _own = own;
    }

    /// <summary>Gets the number of complex types that declare assertions.</summary>
    internal int Count => _own.Count;

    /// <summary>Gets the assertions of a type: those of its base types first, then its own.</summary>
    /// <param name="type">The type of an element.</param>
    /// <returns>The assertions, empty for none.</returns>
    internal IReadOnlyList<ComplexTypeAssertion> Of(IXsdComponent? type) =>
        type is ComplexTypeDefinition complex && _own.TryGetValue(complex, out var own) ? own : [];
}
