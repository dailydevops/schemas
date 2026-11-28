namespace NetEvolve.Schemas.Xsd.Wildcards.Negation;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>Sources of <see cref="IDeclaredNames"/> for <c>##defined</c> and <c>##definedSibling</c>.</summary>
internal static class DeclaredNames
{
    /// <summary>Creates a set from names; later changes of the list are not seen.</summary>
    /// <param name="names">The names.</param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames Of(IReadOnlyList<XPathQName> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        return new NameSet(names);
    }

    /// <summary>
    /// Creates the set of the names a registry holds for one component kind, for <c>##defined</c>: the registry is asked when
    /// the set is, so the set may be handed out before every component is registered.
    /// </summary>
    /// <param name="registry">The registry.</param>
    /// <param name="kind">The kind: the element declarations for <c>any</c>, the attribute declarations for <c>anyAttribute</c>.</param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames Of(IComponentRegistry registry, XsdComponentKind kind)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return new RegistryNames(registry, kind);
    }

    /// <summary>
    /// Creates the set of the element declarations of a content model, for <c>##definedSibling</c> (Structures 3.10.4.1,
    /// clause 3): the declarations that the particles name directly, through nested model groups and group definitions
    /// (indirectly), and, when the substitution groups are given, the members of the substitution group of each of them
    /// (implicitly).
    /// </summary>
    /// <param name="content">The content particle of the complex type, <see langword="null"/> for none.</param>
    /// <param name="groups">
    /// The substitution groups, <see langword="null"/> for none; the members of a declaration that is an
    /// <see cref="ISubstitutionElement"/> are listed too.
    /// </param>
    /// <returns>The set.</returns>
    internal static IDeclaredNames SiblingsOf(Particle? content, SubstitutionGroups? groups = null)
    {
        var names = new List<XPathQName>();
        if (content is not null)
        {
            Collect(content, groups, names, []);
        }

        return new NameSet(names);
    }

    private static void Collect(
        Particle particle,
        SubstitutionGroups? groups,
        List<XPathQName> names,
        HashSet<ModelGroup> visited
    )
    {
        var group = particle.Term switch
        {
            ModelGroup direct => direct,
            ModelGroupDefinition definition => definition.ModelGroup,
            _ => null,
        };

        if (group is null)
        {
            AddElement(particle.Term, groups, names);
            return;
        }

        // A reference cycle between groups is reported by the resolver; it must not end the process here.
        if (!visited.Add(group))
        {
            return;
        }

        for (var i = 0; i < group.Particles.Count; i++)
        {
            Collect(group.Particles[i], groups, names, visited);
        }
    }

    private static void AddElement(IXsdComponent? term, SubstitutionGroups? groups, List<XPathQName> names)
    {
        if (term is not { Kind: XsdComponentKind.ElementDeclaration })
        {
            return;
        }

        names.Add(term.Name);
        if (groups is not null && term is ISubstitutionElement head)
        {
            var members = groups.GetMembers(head);
            for (var i = 0; i < members.Count; i++)
            {
                names.Add(members[i].Name);
            }
        }
    }

    private sealed class NameSet : IDeclaredNames
    {
        private readonly HashSet<XPathQName> _names = [];

        internal NameSet(IReadOnlyList<XPathQName> names)
        {
            for (var i = 0; i < names.Count; i++)
            {
                _ = _names.Add(names[i]);
            }
        }

        public bool Contains(XPathQName name) => _names.Contains(name);
    }

    private sealed class RegistryNames(IComponentRegistry registry, XsdComponentKind kind) : IDeclaredNames
    {
        public bool Contains(XPathQName name) => registry.TryGet(kind, name, out _);
    }
}
