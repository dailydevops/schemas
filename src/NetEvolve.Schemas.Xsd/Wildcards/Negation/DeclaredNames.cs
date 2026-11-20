namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>Sources of <see cref="IDeclaredNames"/>.</summary>
internal static class DeclaredNames
{
    /// <summary>Creates a set from names.</summary>
    /// <param name="names">The names.</param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames Of(IReadOnlyList<XPathQName> names) => throw new NotSupportedException();

    /// <summary>Creates a set of the names a registry holds for one component kind, queried when asked.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames Of(IComponentRegistry registry, XsdComponentKind kind) =>
        throw new NotSupportedException();

    /// <summary>Creates the set of the element declarations that the particles of a content model name.</summary>
    /// <param name="content">The content particle, <see langword="null"/> for none.</param>
    /// <param name="groups">The substitution groups, <see langword="null"/> for none.</param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames SiblingsOf(Particle? content, SubstitutionGroups? groups = null) =>
        throw new NotSupportedException();
}
