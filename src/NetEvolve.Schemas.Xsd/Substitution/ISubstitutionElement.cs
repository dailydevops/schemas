namespace NetEvolve.Schemas.Xsd.Substitution;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The view of an element declaration that substitution groups need (Structures 3.3): the head it is affiliated with,
/// its type, and the abstract, block, and final properties.
/// </summary>
/// <remarks>
/// Consumer-defined seam: the element declaration component (XSD-74) implements it, so this package does not depend on
/// the shape of that component.
/// </remarks>
internal interface ISubstitutionElement : IXsdComponent
{
    /// <summary>Gets the head of the substitution group the declaration is affiliated with, <see langword="null"/> for none.</summary>
    ISubstitutionElement? SubstitutionGroup { get; }

    /// <summary>Gets the type definition of the declaration.</summary>
    IXsdComponent Type { get; }

    /// <summary>Gets a value indicating whether the declaration is abstract.</summary>
    bool IsAbstract { get; }

    /// <summary>Gets the disallowed substitutions (<c>block</c>): any of extension, restriction, and substitution.</summary>
    XsdDerivationMethod Block { get; }

    /// <summary>Gets the substitution group exclusions (<c>final</c>): any of extension and restriction.</summary>
    XsdDerivationMethod Final { get; }
}
