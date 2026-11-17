namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System;

/// <summary>A facet that constrains the lexical form, such as <c>whiteSpace</c> and <c>pattern</c>.</summary>
public interface ILexicalFacet : IFacet
{
    /// <summary>Determines whether a lexical form satisfies the facet.</summary>
    /// <param name="lexical">The lexical form.</param>
    /// <returns><see langword="true"/> when satisfied.</returns>
    bool IsSatisfiedBy(ReadOnlySpan<char> lexical);
}
