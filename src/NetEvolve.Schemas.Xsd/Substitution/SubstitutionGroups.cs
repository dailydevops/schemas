namespace NetEvolve.Schemas.Xsd.Substitution;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// The substitution groups of a set of element declarations (Structures 3.3.6, Substitution Group OK (Transitive) and
/// Substitution Group): the transitive members of each head, respecting block, final, and abstract.
/// </summary>
/// <remarks>
/// Limitations: only one head per element (several heads are XSD-71); the type derivation check is the
/// <see cref="IDerivationCheck"/> seam, asked with <see cref="XsdDerivationMethod.Substitution"/> and a block set of
/// extension and restriction only; the block of the head element and of its type applies to every member of the head,
/// as Structures 1.0 words it, not to each link of the chain; all violations are reported with the rule
/// <c>cos-equiv-derived-ok-rec</c>, which exists in both versions; a declaration whose chain of affiliations does not end
/// is never a member, and only the declarations on the cycle are reported; a declaration whose own affiliation is
/// invalid is reported and is not a member of its head, but declarations affiliated with it keep their own membership
/// in its group and in the groups of the transitive heads.
/// </remarks>
internal sealed class SubstitutionGroups
{
    private const XsdDerivationMethod TypeDerivations = XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly Dictionary<ISubstitutionElement, List<ISubstitutionElement>> _members;

    internal SubstitutionGroups(Dictionary<ISubstitutionElement, List<ISubstitutionElement>> members) =>
        _members = members;

    /// <summary>Computes the substitution groups.</summary>
    /// <param name="elements">The element declarations; their order is the order of the members.</param>
    /// <param name="derivation">The type derivation check.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <returns>The groups.</returns>
    public static SubstitutionGroups Build(
        IReadOnlyList<ISubstitutionElement> elements,
        IDerivationCheck derivation,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(reporter);

        var members = new Dictionary<ISubstitutionElement, List<ISubstitutionElement>>();
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var head = element.SubstitutionGroup;
            if (head is null)
            {
                continue;
            }

            var chain = Walk(element, elements.Count);
            if (chain == Chain.OnCycle)
            {
                Report(reporter, element, "The substitution group affiliation of '" + element.Name + "' is circular.");
            }
            else if (chain == Chain.Open && IsAffiliationValid(element, head, derivation, reporter))
            {
                AddToChain(members, element, head, derivation);
            }
        }

        return new SubstitutionGroups(members);
    }

    /// <summary>Gets the elements that are substitutable for a head: the head itself first, then its transitive members in declaration order.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The members.</returns>
    public IReadOnlyList<ISubstitutionElement> GetMembers(ISubstitutionElement head)
    {
        ArgumentNullException.ThrowIfNull(head);

        return _members.TryGetValue(head, out var list) ? list : [head];
    }

    /// <summary>Gets the elements that may appear in an instance where the head is expected: the members that are not abstract.</summary>
    /// <param name="head">The head.</param>
    /// <returns>The elements.</returns>
    public IReadOnlyList<ISubstitutionElement> GetInstanceElements(ISubstitutionElement head)
    {
        var all = GetMembers(head);
        var result = new List<ISubstitutionElement>(all.Count);
        for (var i = 0; i < all.Count; i++)
        {
            if (!all[i].IsAbstract)
            {
                result.Add(all[i]);
            }
        }

        return result;
    }

    private static Chain Walk(ISubstitutionElement element, int limit)
    {
        var steps = 0;
        for (var current = element.SubstitutionGroup; current is not null; current = current.SubstitutionGroup)
        {
            if (ReferenceEquals(current, element))
            {
                return Chain.OnCycle;
            }

            if (++steps > limit)
            {
                return Chain.LeadsIntoCycle;
            }
        }

        return Chain.Open;
    }

    private static bool IsAffiliationValid(
        ISubstitutionElement element,
        ISubstitutionElement head,
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

    private static void AddToChain(
        Dictionary<ISubstitutionElement, List<ISubstitutionElement>> members,
        ISubstitutionElement element,
        ISubstitutionElement firstHead,
        IDerivationCheck derivation
    )
    {
        for (var head = firstHead; head is not null; head = head.SubstitutionGroup)
        {
            var blocked = head.Block & TypeDerivations;
            if (head.Type is IBlockedType blockedType)
            {
                blocked |= blockedType.Block & TypeDerivations;
            }

            if (
                (head.Block & XsdDerivationMethod.Substitution) != XsdDerivationMethod.None
                || !Derives(element, head, derivation, blocked)
            )
            {
                continue;
            }

            if (!members.TryGetValue(head, out var list))
            {
                members[head] = list = [head];
            }

            list.Add(element);
        }
    }

    private static bool Derives(
        ISubstitutionElement element,
        ISubstitutionElement head,
        IDerivationCheck derivation,
        XsdDerivationMethod blockSet
    ) => derivation.Check(element.Type, head.Type, XsdDerivationMethod.Substitution, blockSet).IsValid;

    private static void Report(XsdIssueReporter reporter, ISubstitutionElement element, string message) =>
        reporter.Report(XsdRuleLookup.CosEquivDerivedOkRec, element.Location, message);

    private enum Chain
    {
        Open,
        OnCycle,
        LeadsIntoCycle,
    }
}
