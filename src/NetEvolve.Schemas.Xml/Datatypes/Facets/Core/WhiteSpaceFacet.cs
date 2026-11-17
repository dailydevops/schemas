namespace NetEvolve.Schemas.Xml.Datatypes.Facets.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>The <c>whiteSpace</c> facet (XSD 1.1 Part 2, section 4.3.6): a lexical form satisfies it when it is already in the normal form of the mode.</summary>
/// <remarks>The descriptors apply the mode before the other facets, so on a descriptor this facet always holds; it documents and checks the normal form.</remarks>
public sealed class WhiteSpaceFacet : ILexicalFacet
{
    /// <summary>Initializes a new instance of the <see cref="WhiteSpaceFacet"/> class.</summary>
    /// <param name="mode">The whitespace mode.</param>
    public WhiteSpaceFacet(WhitespaceMode mode) => Mode = mode;

    /// <inheritdoc />
    public FacetKind Kind => FacetKind.WhiteSpace;

    /// <summary>Gets the whitespace mode.</summary>
    public WhitespaceMode Mode { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(ReadOnlySpan<char> lexical)
    {
        if (Mode == WhitespaceMode.Preserve)
        {
            return true;
        }

        for (var i = 0; i < lexical.Length; i++)
        {
            var c = lexical[i];
            if (c is '\t' or '\n' or '\r')
            {
                return false;
            }

            if (
                Mode == WhitespaceMode.Collapse
                && c == ' '
                && (i == 0 || i == lexical.Length - 1 || lexical[i - 1] == ' ')
            )
            {
                return false;
            }
        }

        return true;
    }
}
