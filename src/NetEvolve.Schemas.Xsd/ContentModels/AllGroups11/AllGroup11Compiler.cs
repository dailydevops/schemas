namespace NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>
/// Builds the XSD 1.1 <c>all</c> group program from resolved particles, and merges the content of an extension with the content
/// of its base type (Structures 1.1, 3.4.2 clause 4.2.3.2, 3.8.6.2 and 3.9).
/// </summary>
/// <remarks>
/// <para>
/// A group reference to an <c>all</c> group is the group itself: the definition is unwrapped, so a reference compiles to the
/// program of the inline group. A particle of an <c>all</c> group whose term is another <c>all</c> group, directly or through a
/// definition, has <c>minOccurs</c> and <c>maxOccurs</c> of 1 (All Group Limited, clause 1.3), so its members are flattened into
/// the enclosing group; interleaving interleavings is interleaving.
/// </para>
/// <para>
/// Limitations: this compiler is not wired into the validation model; that is XSD-78, which also has to call
/// <see cref="Extend"/> where a complex type is derived by extension, because the construction of the complex type still
/// combines the contents as a sequence. A nested <c>all</c> group with another occurrence (a schema error, XSD-60), and a member
/// that is neither an element declaration nor a wildcard, is left out. A substitution group closure of a member is not
/// expanded; a member accepts the name of its declaration only. Nesting deeper than <see cref="MaxNesting"/> levels is an
/// internal, non-configurable ceiling; such a group is not compiled.
/// </para>
/// </remarks>
internal static class AllGroup11Compiler
{
    /// <summary>The deepest nesting of <c>all</c> groups in <c>all</c> groups that is compiled.</summary>
    internal const int MaxNesting = 64;

    /// <summary>Compiles the content particle of a complex type when its term is an <c>all</c> group.</summary>
    /// <param name="content">The particle, with a model group or a group definition as term.</param>
    /// <param name="idOf">Gives the schema name id of the name of an element declaration.</param>
    /// <param name="terms">Receives the term of each member, so the index of a matched particle finds its component.</param>
    /// <returns>The program, <see langword="null"/> when the particle cannot occur, is no <c>all</c> group, or nests too deep.</returns>
    internal static AllGroup11? Compile(Particle content, Func<XPathQName, int> idOf, List<IXsdComponent> terms)
    {
        Argument.ThrowIfNull(content);
        Argument.ThrowIfNull(idOf);
        Argument.ThrowIfNull(terms);

        if (
            content.MaxOccurs == 0
            || Unwrap(content.Term) is not ModelGroup { Compositor: ModelGroupCompositor.All } group
        )
        {
            return null;
        }

        var members = new List<AutomatonParticle>();
        var memberTerms = new List<IXsdComponent>();
        if (!Collect(group, 1, idOf, members, memberTerms))
        {
            return null;
        }

        terms.AddRange(memberTerms);
        return new AllGroup11(content.MinOccurs == 0, members.ToArray());
    }

    /// <summary>
    /// Merges the particle of a base type with the explicit content of an extension when both are <c>all</c> groups (clause
    /// 4.2.3.2): one <c>all</c> group with the members of the base first and then the own members.
    /// </summary>
    /// <param name="baseParticle">The particle of the content type of the base type.</param>
    /// <param name="content">The explicit content of the extension.</param>
    /// <returns>
    /// The merged particle with the <c>minOccurs</c> of <paramref name="content"/> and <c>maxOccurs</c> 1, or
    /// <see langword="null"/> when either is no <c>all</c> group; the extension then is a sequence of both (clause 4.2.3.3).
    /// </returns>
    internal static Particle? Extend(Particle baseParticle, Particle content)
    {
        Argument.ThrowIfNull(baseParticle);
        Argument.ThrowIfNull(content);

        if (
            Unwrap(baseParticle.Term) is not ModelGroup { Compositor: ModelGroupCompositor.All } baseGroup
            || Unwrap(content.Term) is not ModelGroup { Compositor: ModelGroupCompositor.All } ownGroup
        )
        {
            return null;
        }

        var particles = new List<Particle>(baseGroup.Particles.Count + ownGroup.Particles.Count);
        particles.AddRange(baseGroup.Particles);
        particles.AddRange(ownGroup.Particles);
        return Particle.Create(
            content.Location,
            content.MinOccurs,
            1,
            ModelGroup.Create(ModelGroupCompositor.All, content.Location, particles)
        );
    }

    private static bool Collect(
        ModelGroup group,
        int depth,
        Func<XPathQName, int> idOf,
        List<AutomatonParticle> members,
        List<IXsdComponent> terms
    )
    {
        if (depth > MaxNesting)
        {
            return false;
        }

        foreach (var particle in group.Particles)
        {
            if (particle.MaxOccurs == 0 || particle.MinOccurs > particle.MaxOccurs)
            {
                continue;
            }

            switch (Unwrap(particle.Term))
            {
                case ElementDeclaration element:
                    members.Add(AutomatonParticle.Element(particle.MinOccurs, particle.MaxOccurs, idOf(element.Name)));
                    terms.Add(element);
                    break;
                case IWildcard wildcard:
                    members.Add(AutomatonParticle.Wildcard(particle.MinOccurs, particle.MaxOccurs, wildcard));
                    terms.Add(wildcard);
                    break;
                case ModelGroup { Compositor: ModelGroupCompositor.All } nested
                    when particle.MinOccurs == 1 && particle.MaxOccurs == 1:
                    if (!Collect(nested, depth + 1, idOf, members, terms))
                    {
                        return false;
                    }

                    break;
                default:
                    break;
            }
        }

        return true;
    }

    private static IXsdComponent? Unwrap(IXsdComponent? term) =>
        term is ModelGroupDefinition definition ? definition.ModelGroup : term;
}
