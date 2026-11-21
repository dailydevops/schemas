namespace NetEvolve.Schemas.Xsd.Declarations11;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>
/// An element declaration of XSD 1.1 that may be affiliated with several substitution group heads (Structures 1.1, 3.3.1,
/// <c>{substitution group affiliations}</c>).
/// </summary>
/// <remarks>
/// Consumer-defined seam: the element declaration component implements it when the 1.1 pipeline is wired (#360).
/// <see cref="ISubstitutionElement.SubstitutionGroup"/> is not read by <see cref="SubstitutionGroups11"/>; <see cref="Heads"/>
/// is the one source of the affiliations.
/// </remarks>
internal interface IMultiHeadElement : ISubstitutionElement
{
    /// <summary>Gets the heads of the substitution groups the declaration is affiliated with, in the order the schema lists them; empty for none.</summary>
    IReadOnlyList<IMultiHeadElement> Heads { get; }
}
