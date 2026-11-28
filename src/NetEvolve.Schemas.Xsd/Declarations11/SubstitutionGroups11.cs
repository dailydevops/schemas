namespace NetEvolve.Schemas.Xsd.Declarations11;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>
/// The substitution groups of declarations that may be affiliated with several heads (Structures 1.1, 3.3.6.1 Element
/// Declaration Properties Correct, clauses 4 and 5, and 3.3.6.3 Substitution Group OK (Transitive)): the transitive members
/// of each head, respecting block, final, and abstract.
/// </summary>
/// <remarks>
/// <para>
/// The computation of <see cref="SubstitutionGroups"/> for one head, extended to a graph of affiliations. The type of a
/// declaration must be validly derived from the type of every head it names, subject to the <c>final</c> of that head; the
/// block of a head element and of its type applies to every member of the head, as Structures 1.1 words it, not to each link of
/// a chain.
/// </para>
/// <para>
/// Decisions on #322: a violation against one head is reported once for that head with the rule
/// <c>cos-equiv-derived-ok-rec</c>, as in #239, and does not remove the declaration from the groups of its other heads; a
/// declaration is a member of a group once, however many chains lead to the head; a declaration that can reach itself through
/// any of its heads is on a circle, is reported (<c>cos-equiv-derived-ok-rec</c>), and is never a member; a declaration whose
/// heads lead into a circle, without being on it, is not reported and is never a member; a declaration whose own affiliation is
/// invalid is reported and is not a member of that head, but declarations affiliated with it keep their own membership in its
/// group and in the groups of its heads.
/// </para>
/// <para>
/// Limitations: the type derivation check is the <see cref="IDerivationCheck"/> seam, asked with
/// <see cref="XsdDerivationMethod.Substitution"/> and a block set of extension and restriction only. The compiled
/// validator reads the groups through <see cref="ToSubstitutionGroups"/>.
/// </para>
/// </remarks>
internal sealed class SubstitutionGroups11
{
    private const XsdDerivationMethod TypeDerivations = XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly Dictionary<IMultiHeadElement, List<IMultiHeadElement>> _members;

    private SubstitutionGroups11(Dictionary<IMultiHeadElement, List<IMultiHeadElement>> members) => _members = members;

    /// <summary>Computes the substitution groups.</summary>
    /// <param name="elements">The element declarations; their order is the order of the members.</param>
    /// <param name="derivation">The type derivation check.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <returns>The groups.</returns>
    public static SubstitutionGroups11 Build(
        IReadOnlyList<IMultiHeadElement> elements,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(reporter);

        var reach = new Dictionary<IMultiHeadElement, HashSet<IMultiHeadElement>>();
        var members = new Dictionary<IMultiHeadElement, List<IMultiHeadElement>>();
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            if (element.Heads.Count == 0)
            {
                continue;
            }

            var heads = Reach(element, reach);
            if (heads.Contains(element))
            {
                Report(reporter, element, "The substitution group affiliation of '" + element.Name + "' is circular.");
            }
            else if (!LeadsIntoCircle(heads, reach))
            {
                Join(members, element, reach, derivation, reporter);
            }
        }

        return new SubstitutionGroups11(members);
    }

    /// <summary>Gets the groups as the single-head view that the validator and the particle checks read.</summary>
    /// <returns>The same members for every head, in the same order.</returns>
    internal SubstitutionGroups ToSubstitutionGroups()
    {
        var members = new Dictionary<ISubstitutionElement, List<ISubstitutionElement>>(_members.Count);
        foreach (var group in _members)
        {
            members[group.Key] = [.. group.Value];
        }

        return new SubstitutionGroups(members);
    }

    /// <summary>Gets the elements that are substitutable for a head: the head itself first, then its transitive members in declaration order.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The members.</returns>
    public IReadOnlyList<IMultiHeadElement> GetMembers(IMultiHeadElement head)
    {
        ArgumentNullException.ThrowIfNull(head);

        return _members.TryGetValue(head, out var list) ? list : [head];
    }

    /// <summary>Gets the elements that may appear in an instance where the head is expected: the members that are not abstract.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The elements.</returns>
    public IReadOnlyList<IMultiHeadElement> GetInstanceElements(IMultiHeadElement head)
    {
        var all = GetMembers(head);
        var result = new List<IMultiHeadElement>(all.Count);
        for (var i = 0; i < all.Count; i++)
        {
            if (!all[i].IsAbstract)
            {
                result.Add(all[i]);
            }
        }

        return result;
    }

    // Every declaration that a chain of affiliations from the element reaches, found once and kept.
    private static HashSet<IMultiHeadElement> Reach(
        IMultiHeadElement element,
        Dictionary<IMultiHeadElement, HashSet<IMultiHeadElement>> cache
    )
    {
        if (cache.TryGetValue(element, out var known))
        {
            return known;
        }

        var seen = new HashSet<IMultiHeadElement>();
        var pending = new Stack<IMultiHeadElement>();
        pending.Push(element);
        while (pending.Count > 0)
        {
            var heads = pending.Pop().Heads;
            for (var i = 0; i < heads.Count; i++)
            {
                if (seen.Add(heads[i]))
                {
                    pending.Push(heads[i]);
                }
            }
        }

        cache[element] = seen;
        return seen;
    }

    private static bool LeadsIntoCircle(
        HashSet<IMultiHeadElement> heads,
        Dictionary<IMultiHeadElement, HashSet<IMultiHeadElement>> cache
    )
    {
        foreach (var head in heads)
        {
            if (Reach(head, cache).Contains(head))
            {
                return true;
            }
        }

        return false;
    }

    // Each named head is checked once; a valid one brings the member into the group of the head and of every head above it.
    private static void Join(
        Dictionary<IMultiHeadElement, List<IMultiHeadElement>> members,
        IMultiHeadElement element,
        Dictionary<IMultiHeadElement, HashSet<IMultiHeadElement>> cache,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        var joined = new HashSet<IMultiHeadElement>();
        var checkedHeads = new HashSet<IMultiHeadElement>();
        for (var i = 0; i < element.Heads.Count; i++)
        {
            var head = element.Heads[i];
            if (!checkedHeads.Add(head) || !IsAffiliationValid(element, head, derivation, reporter))
            {
                continue;
            }

            Add(members, element, head, joined, derivation);
            foreach (var above in Reach(head, cache))
            {
                Add(members, element, above, joined, derivation);
            }
        }
    }

    private static void Add(
        Dictionary<IMultiHeadElement, List<IMultiHeadElement>> members,
        IMultiHeadElement element,
        IMultiHeadElement head,
        HashSet<IMultiHeadElement> joined,
        IDerivationCheck derivation
    )
    {
        if (!joined.Add(head) || !IsSubstitutable(element, head, derivation))
        {
            return;
        }

        if (!members.TryGetValue(head, out var list))
        {
            members[head] = list = [head];
        }

        list.Add(element);
    }

    private static bool IsAffiliationValid(
        IMultiHeadElement element,
        IMultiHeadElement head,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        if (!Derives(element, head, derivation, XsdDerivationMethod.None))
        {
            Report(
                reporter,
                element,
                "The type of '"
                    + element.Name
                    + "' is not derived from the type of its substitution group head '"
                    + head.Name
                    + "'."
            );
            return false;
        }

        if (!Derives(element, head, derivation, head.Final & TypeDerivations))
        {
            Report(
                reporter,
                element,
                "The substitution group head '"
                    + head.Name
                    + "' is final for the derivation of the type of '"
                    + element.Name
                    + "'."
            );
            return false;
        }

        return true;
    }

    private static bool IsSubstitutable(IMultiHeadElement element, IMultiHeadElement head, IDerivationCheck derivation)
    {
        if ((head.Block & XsdDerivationMethod.Substitution) != XsdDerivationMethod.None)
        {
            return false;
        }

        var blocked = head.Block & TypeDerivations;
        if (head.Type is IBlockedType blockedType)
        {
            blocked |= blockedType.Block & TypeDerivations;
        }

        return Derives(element, head, derivation, blocked);
    }

    private static bool Derives(
        IMultiHeadElement element,
        IMultiHeadElement head,
        IDerivationCheck derivation,
        XsdDerivationMethod blockSet
    ) => derivation.Check(element.Type, head.Type, XsdDerivationMethod.Substitution, blockSet).IsValid;

    private static void Report(XsdIssueReporter reporter, IMultiHeadElement element, string message) =>
        reporter.Report(XsdRuleLookup.CosEquivDerivedOkRec, element.Location, message);
}
