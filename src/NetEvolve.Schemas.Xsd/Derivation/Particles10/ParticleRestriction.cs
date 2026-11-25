namespace NetEvolve.Schemas.Xsd.Derivation.Particles10;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Substitution;

/// <summary>
/// Particle Valid (Restriction) (<c>cos-particle-restrict</c>, Structures 1.0, 3.9.6): whether a particle is a valid restriction of
/// another by the structure of both, with every case of the table and the constraints it refers to (Occurrence Range OK, the
/// element, wildcard, and group cases, Particle Emptiable, and the effective total range).
/// </summary>
/// <remarks>
/// <para>
/// The check is a pure function of immutable components and keeps no state, so one instance serves any number of calls. It is not
/// yet part of the content check of a derived type (clause 5.4.2 of <c>derivation-ok-restriction</c>), which the pipeline of
/// XSD-75 composes for XSD 1.0 beside the subsumption check of XSD 1.1; a caller passes the content particles of both types.
/// </para>
/// <para>
/// Reported violations: each one names the rule of the constraint that failed and the numbered clause, and is located at the
/// restricting particle that failed (after pointless groups are ignored, see <see cref="ParticleStructure"/>); an occurrence
/// range outside the base range is reported with <c>range-ok</c>, also where a case demands it as one of its clauses; a failure
/// inside a group case is reported with the rule of the group case at the member that could not be mapped (the first one in
/// order), without the detail of the member, except where a member is compared with a wildcard (<c>rcase-NSRecurseCheckCardinality.1</c>,
/// the message carries the member's violation). An element that is compared with a group is reported with
/// <c>rcase-RecurseAsIfGroup</c> at the element.
/// </para>
/// <para>
/// Limitations: a particle whose term is not resolved is not compared (the resolver reports it); a circular group definition is not
/// compared (<c>mg-props-correct.2</c>); an element declaration that heads a substitution group stands for a choice of its
/// instance members when it is compared with an element of another name, but only when <see cref="Substitutions"/> is set, otherwise it is
/// compared as itself; clause 3.2.3 of <c>rcase-NameAndTypeOK</c> (the identity
/// constraints of the restriction are a subset of those of the base) is not checked, because an element declaration keeps no
/// identity constraints yet; the fixed values of <c>rcase-NameAndTypeOK.3.2.2</c> are compared in the value space of the
/// element types (a declaration without a type compares the lexical forms); a type that an element declaration does not know yet (an unresolved reference, or an anonymous type
/// that is not constructed) is not compared, and an element declaration without a type has the ur-type; a restriction that cannot
/// map the members of an <c>all</c> group of the base by name (a base that is not unique, which <c>cos-nonambig</c> reports)
/// takes the first free member that fits; the occurrence limitations of <see cref="OccurrenceRange"/> apply. The recursion over
/// nested groups is bounded by the depth of the schema documents, see <see cref="ParticleStructure"/>.
/// </para>
/// </remarks>
internal sealed class ParticleRestriction
{
    private static readonly IXsdComponent UrTypeWildcard = FindUrTypeWildcard();

    private static readonly ConditionalWeakTable<Particle, object> SelfStanding = new();

    private readonly TypeDerivationOk _derivation;
    private readonly ValueSpace _values;

    /// <summary>Initializes a new instance of the <see cref="ParticleRestriction"/> class.</summary>
    /// <param name="derivation">Type Derivation OK of XSD 1.0, for the types of element declarations.</param>
    /// <param name="values">The value space, which compares the fixed values of element declarations.</param>
    internal ParticleRestriction(TypeDerivationOk derivation, ValueSpace values)
    {
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(values);

        _derivation = derivation;
        _values = values;
    }

    /// <summary>
    /// Gets or sets the substitution groups of the schema, <see langword="null"/> when the element declarations are compared as themselves.
    /// The pipeline sets it once before the particles are compared.
    /// </summary>
    internal SubstitutionGroups? Substitutions { get; set; }

    /// <summary>Checks a particle against the particle it restricts.</summary>
    /// <param name="restriction">The restricting particle, for example the content particle of a derived type.</param>
    /// <param name="baseParticle">The particle of the base.</param>
    /// <returns>The first violated constraint, none when the particle is a valid restriction.</returns>
    internal ParticleViolation Check(Particle restriction, Particle baseParticle)
    {
        ArgumentNullException.ThrowIfNull(restriction);
        ArgumentNullException.ThrowIfNull(baseParticle);

        // The recursion needs finite structures; the resolver reports a circular group definition.
        return ParticleStructure.IsCircular(restriction) || ParticleStructure.IsCircular(baseParticle)
            ? ParticleViolation.None
            : Valid(restriction, baseParticle);
    }

    /// <summary>Checks a particle against the particle it restricts and reports a violation as a located issue.</summary>
    /// <param name="restriction">The restricting particle.</param>
    /// <param name="baseParticle">The particle of the base.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(Particle restriction, Particle baseParticle, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var violation = Check(restriction, baseParticle);
        if (violation.IsViolation)
        {
            reporter.Report(
                XsdRuleLookup.Find(violation.Code),
                violation.Location,
                violation.Message!,
                violation.Clause
            );
        }
    }

    private static IXsdComponent FindUrTypeWildcard() =>
        ((ModelGroup)ComplexTypeDefinition.AnyType.ContentParticle!.Term!).Particles[0].Term!;

    private static ParticleViolation Fail(DiagnosticCode code, string clause, Particle at, string message) =>
        new(code, clause, message, at.Location);

    private static string Show(long occurrence) =>
        occurrence == Particle.Unbounded ? "unbounded" : occurrence.ToString(CultureInfo.InvariantCulture);

    private static string Describe(Particle particle) =>
        particle.Term switch
        {
            ElementDeclaration element => "the element '" + element.Name.LocalName + "'",
            IWildcard => "a wildcard",
            _ => ParticleStructure.GroupOf(particle)!.Compositor switch
            {
                ModelGroupCompositor.All => "an all group",
                ModelGroupCompositor.Choice => "a choice group",
                _ => "a sequence group",
            },
        };

    // Occurrence Range OK: the minimum is not below and the maximum not above the base range; unbounded is the highest value.
    private static ParticleViolation Range(Particle at, long min, long max, Particle baseParticle, string what)
    {
        if (min < baseParticle.MinOccurs)
        {
            return Fail(
                XsdRuleCodes.RangeOk,
                "range-ok.1",
                at,
                "The minimum occurrence "
                    + Show(min)
                    + " of "
                    + what
                    + " is below the minimum "
                    + Show(baseParticle.MinOccurs)
                    + " of the base particle."
            );
        }

        return max > baseParticle.MaxOccurs
            ? Fail(
                XsdRuleCodes.RangeOk,
                "range-ok.2",
                at,
                "The maximum occurrence "
                    + Show(max)
                    + " of "
                    + what
                    + " is above the maximum "
                    + Show(baseParticle.MaxOccurs)
                    + " of the base particle."
            )
            : ParticleViolation.None;
    }

    private static ParticleViolation Range(Particle restriction, Particle baseParticle) =>
        Range(restriction, restriction.MinOccurs, restriction.MaxOccurs, baseParticle, "the particle");

    private static ParticleViolation Forbidden(Particle restriction, Particle baseParticle) =>
        Fail(
            XsdRuleCodes.CosParticleRestrict,
            "cos-particle-restrict.2",
            restriction,
            "The table of Particle Valid (Restriction) forbids "
                + Describe(restriction)
                + " as a restriction of "
                + Describe(baseParticle)
                + "."
        );

    private static IXsdComponent? TypeOf(ElementDeclaration element) =>
        element.TypeDefinition
        ?? (element.InlineTypeSyntax is null && element.ReferenceCount == 0 ? ComplexTypeDefinition.AnyType : null);

    // The fixed values are equal in the value space of the types; a declaration without a type compares the lexical forms.
    private bool SameFixedValue(ElementDeclaration element, ElementDeclaration baseElement) =>
        TypeOf(element) is { } type && TypeOf(baseElement) is { } baseType
            ? _values.AreEqual(
                type,
                (element.ValueConstraint.Value ?? string.Empty).AsSpan(),
                baseType,
                (baseElement.ValueConstraint.Value ?? string.Empty).AsSpan()
            )
            : string.Equals(element.ValueConstraint.Value, baseElement.ValueConstraint.Value, StringComparison.Ordinal);

    private ParticleViolation Valid(Particle restriction, Particle baseParticle)
    {
        // Clause 1: the same particle.
        if (ReferenceEquals(restriction, baseParticle))
        {
            return ParticleViolation.None;
        }

        var r = ParticleStructure.Simplify(restriction);
        var b = ParticleStructure.Simplify(baseParticle);
        if (r is not null && b is not null)
        {
            (r, b) = (Expand(r, b, isRestriction: true), Expand(b, r, isRestriction: false));
        }

        if (r is null)
        {
            return b is null || ParticleRules.IsEmptiable(b)
                ? ParticleViolation.None
                : Fail(
                    XsdRuleCodes.CosParticleRestrict,
                    "cos-particle-restrict.2",
                    restriction,
                    "An empty group restricts only a base particle that may be empty."
                );
        }

        if (b is null)
        {
            return Fail(
                XsdRuleCodes.CosParticleRestrict,
                "cos-particle-restrict.2",
                r,
                "The base particle is an empty group, which only an empty restriction can restrict."
            );
        }

        if (ReferenceEquals(r, b))
        {
            return ParticleViolation.None;
        }

        // The table of Particle Valid (Restriction); a term that is not resolved is not compared.
        return (r.Term, b.Term) switch
        {
            (ElementDeclaration, ElementDeclaration) => NameAndTypeOK(r, b),
            (ElementDeclaration, IWildcard) => NSCompat(r, b),
            (ElementDeclaration, ModelGroup or ModelGroupDefinition) => RecurseAsIfGroup(r, b),
            (IWildcard, IWildcard) => NSSubset(r, b),
            (IWildcard, ElementDeclaration or ModelGroup or ModelGroupDefinition) => Forbidden(r, b),
            (ModelGroup or ModelGroupDefinition, ElementDeclaration) => Forbidden(r, b),
            (ModelGroup or ModelGroupDefinition, IWildcard) => NSRecurseCheckCardinality(r, b),
            (ModelGroup or ModelGroupDefinition, ModelGroup or ModelGroupDefinition) => Groups(r, b),
            _ => ParticleViolation.None,
        };
    }

    // An element declaration that heads a substitution group stands for a choice of its instance members, unless the other particle
    // is the same element; a restriction is expanded only against another element, because a group or a wildcard of the base
    // is compared with the element itself. The choice keeps the occurrence of the particle.
    private Particle Expand(Particle particle, Particle other, bool isRestriction)
    {
        if (
            Substitutions is null
            || SelfStanding.TryGetValue(particle, out _)
            || particle.Term is not ElementDeclaration { IsGlobal: true } head
            || (other.Term is ElementDeclaration otherElement && head.Name == otherElement.Name)
            || (isRestriction && other.Term is not ElementDeclaration)
        )
        {
            return particle;
        }

        var instances = Substitutions.GetInstanceElements(head);
        if (instances.Count == 1 && ReferenceEquals(instances[0], head))
        {
            return particle;
        }

        // The members stand for an unordered choice, so both sides list them in the same order: by name.
        var ordered = new List<ISubstitutionElement>(instances);
        ordered.Sort(static (x, y) => CompareNames(x.Name, y.Name));
        var members = new List<Particle>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            // A member stands for itself; it is not expanded again.
            var member = Particle.Create(particle.Location, 1, 1, (ElementDeclaration)ordered[i]);
            SelfStanding.Add(member, member);
            members.Add(member);
        }

        return Particle.Create(
            particle.Location,
            particle.MinOccurs,
            particle.MaxOccurs,
            ModelGroup.Create(ModelGroupCompositor.Choice, particle.Location, members)
        );
    }

    private static int CompareNames(XPathQName x, XPathQName y)
    {
        var space = string.CompareOrdinal(x.NamespaceName, y.NamespaceName);
        return space != 0 ? space : string.CompareOrdinal(x.LocalName, y.LocalName);
    }

    private ParticleViolation NameAndTypeOK(Particle r, Particle b)
    {
        var element = (ElementDeclaration)r.Term!;
        var baseElement = (ElementDeclaration)b.Term!;
        if (element.Name != baseElement.Name)
        {
            return Fail(
                XsdRuleCodes.RcaseNameAndTypeOK,
                "rcase-NameAndTypeOK.1",
                r,
                "The element '"
                    + element.Name.LocalName
                    + "' is not the element '"
                    + baseElement.Name.LocalName
                    + "' of the base particle."
            );
        }

        // Clause 3.1: two global declarations are compared by name and occurrence only.
        var range = Range(r, b);
        if (range.IsViolation || (element.IsGlobal && baseElement.IsGlobal))
        {
            return range;
        }

        var scope = CheckLocalScope(r, element, baseElement);
        return scope.IsViolation ? scope : CheckType(r, element, baseElement);
    }

    // Clauses 3.2.1 to 3.2.4; clause 3.2.3 (identity constraints) is not checked.
    private ParticleViolation CheckLocalScope(Particle r, ElementDeclaration element, ElementDeclaration baseElement)
    {
        if (element.IsNillable && !baseElement.IsNillable)
        {
            return Fail(
                XsdRuleCodes.RcaseNameAndTypeOK,
                "rcase-NameAndTypeOK.3.2.1",
                r,
                "The element '" + element.Name.LocalName + "' is nillable but the element of the base particle is not."
            );
        }

        if (
            baseElement.ValueConstraint.Kind == ElementValueConstraintKind.Fixed
            && (
                element.ValueConstraint.Kind != ElementValueConstraintKind.Fixed
                || !SameFixedValue(element, baseElement)
            )
        )
        {
            return Fail(
                XsdRuleCodes.RcaseNameAndTypeOK,
                "rcase-NameAndTypeOK.3.2.2",
                r,
                "The element '"
                    + element.Name.LocalName
                    + "' is fixed in the base particle and must keep the same fixed value."
            );
        }

        return (baseElement.Block & ~element.Block) != XsdDerivationMethod.None
            ? Fail(
                XsdRuleCodes.RcaseNameAndTypeOK,
                "rcase-NameAndTypeOK.3.2.4",
                r,
                "The disallowed substitutions of the element '"
                    + element.Name.LocalName
                    + "' must include those of the element of the base particle."
            )
            : ParticleViolation.None;
    }

    // Clause 3.2.5: derived by restriction, so a step by extension is blocked (list and union never count for a type).
    private ParticleViolation CheckType(Particle r, ElementDeclaration element, ElementDeclaration baseElement)
    {
        if (TypeOf(element) is not { } type || TypeOf(baseElement) is not { } baseType)
        {
            return ParticleViolation.None;
        }

        var violation = _derivation.Check(type, baseType, XsdDerivationMethod.Extension);
        return violation.IsViolation
            ? Fail(
                XsdRuleCodes.RcaseNameAndTypeOK,
                "rcase-NameAndTypeOK.3.2.5",
                r,
                "The type of the element '"
                    + element.Name.LocalName
                    + "' must be derived by restriction from the type of the element of the base particle: "
                    + violation.Message
            )
            : ParticleViolation.None;
    }

    private static ParticleViolation NSCompat(Particle r, Particle b)
    {
        var element = (ElementDeclaration)r.Term!;
        if (!((IWildcard)b.Term!).Allows(element.Name))
        {
            return Fail(
                XsdRuleCodes.RcaseNSCompat,
                "rcase-NSCompat.1",
                r,
                "The namespace of the element '"
                    + element.Name.LocalName
                    + "' is not allowed by the wildcard of the base particle."
            );
        }

        return Range(r, b);
    }

    private static ParticleViolation NSSubset(Particle r, Particle b)
    {
        var range = Range(r, b);
        if (range.IsViolation)
        {
            return range;
        }

        var wildcard = (IWildcard)r.Term!;
        var baseWildcard = (IWildcard)b.Term!;
        if (!wildcard.IsSubsetOf(baseWildcard))
        {
            return Fail(
                XsdRuleCodes.RcaseNSSubset,
                "rcase-NSSubset.2",
                r,
                "The namespace constraint of the wildcard must be a subset of that of the base particle."
            );
        }

        // Strict, lax, skip are ordered from the strongest to the weakest; the wildcard of the ur-type is lax and exempt.
        return !ReferenceEquals(baseWildcard, UrTypeWildcard) && wildcard.ProcessContents > baseWildcard.ProcessContents
            ? Fail(
                XsdRuleCodes.RcaseNSSubset,
                "rcase-NSSubset.3",
                r,
                "The process contents of the wildcard must be identical to or stronger than that of the base particle."
            )
            : ParticleViolation.None;
    }

    private ParticleViolation RecurseAsIfGroup(Particle r, Particle b)
    {
        // A group of the kind of the base that occurs once and holds the element must restrict the base group.
        var baseGroup = ParticleStructure.GroupOf(b)!;
        var baseMembers = ParticleStructure.Members(baseGroup);
        List<Particle> single = [r];
        var inner =
            baseGroup.Compositor == ModelGroupCompositor.Choice
                ? RecurseLax(r, 1, 1, single, b, baseMembers)
                : Recurse(r, 1, 1, single, b, baseMembers);
        return inner.IsViolation
            ? Fail(
                XsdRuleCodes.RcaseRecurseAsIfGroup,
                "rcase-RecurseAsIfGroup",
                r,
                "The element '"
                    + ((ElementDeclaration)r.Term!).Name.LocalName
                    + "' is not a valid restriction of the group of the base particle: "
                    + inner.Message
            )
            : ParticleViolation.None;
    }

    private ParticleViolation NSRecurseCheckCardinality(Particle r, Particle b)
    {
        var members = ParticleStructure.Members(ParticleStructure.GroupOf(r)!);
        for (var i = 0; i < members.Count; i++)
        {
            var member = Valid(members[i], b);
            if (member.IsViolation)
            {
                return Fail(
                    XsdRuleCodes.RcaseNSRecurseCheckCardinality,
                    "rcase-NSRecurseCheckCardinality.1",
                    members[i],
                    "A member of the group is not a valid restriction of the wildcard of the base particle: "
                        + member.Message
                );
            }
        }

        OccurrenceRange.EffectiveTotalRange(r, out var min, out var max);
        return Range(r, min, max, b, "the effective total range of the group");
    }

    private ParticleViolation Groups(Particle r, Particle b)
    {
        var group = ParticleStructure.GroupOf(r)!;
        var baseGroup = ParticleStructure.GroupOf(b)!;
        var members = ParticleStructure.Members(group);
        var baseMembers = ParticleStructure.Members(baseGroup);
        switch (group.Compositor, baseGroup.Compositor)
        {
            case (ModelGroupCompositor.All, ModelGroupCompositor.All)
            or (ModelGroupCompositor.Sequence, ModelGroupCompositor.Sequence):
                return Recurse(r, r.MinOccurs, r.MaxOccurs, members, b, baseMembers);
            case (ModelGroupCompositor.Choice, ModelGroupCompositor.Choice):
                return RecurseLax(r, r.MinOccurs, r.MaxOccurs, members, b, baseMembers);
            case (ModelGroupCompositor.Sequence, ModelGroupCompositor.All):
                return RecurseUnordered(r, members, b, baseMembers);
            case (ModelGroupCompositor.Sequence, ModelGroupCompositor.Choice):
                return MapAndSum(r, members, b, baseMembers);
            default:
                return Forbidden(r, b);
        }
    }

    // All:All and Sequence:Sequence: the members map in order to base members, and the base members left out may be empty.
    private ParticleViolation Recurse(
        Particle r,
        long min,
        long max,
        List<Particle> members,
        Particle b,
        List<Particle> baseMembers
    )
    {
        var range = Range(r, min, max, b, "the group");
        if (range.IsViolation)
        {
            return range;
        }

        var next = 0;
        for (var i = 0; i < members.Count; i++)
        {
            var mapped = false;
            while (next < baseMembers.Count)
            {
                var candidate = baseMembers[next++];
                if (!Valid(members[i], candidate).IsViolation)
                {
                    mapped = true;
                    break;
                }

                // The candidate is skipped, and no later member of the restriction can map to it.
                if (!ParticleRules.IsEmptiable(candidate))
                {
                    return Fail(
                        XsdRuleCodes.RcaseRecurse,
                        "rcase-Recurse.2.2",
                        members[i],
                        Describe(members[i])
                            + " does not restrict the next base particle in order, which is not emptiable and so cannot be left out."
                    );
                }
            }

            if (!mapped)
            {
                return Fail(
                    XsdRuleCodes.RcaseRecurse,
                    "rcase-Recurse.2.1",
                    members[i],
                    Describe(members[i]) + " has no base particle left to restrict."
                );
            }
        }

        for (; next < baseMembers.Count; next++)
        {
            if (!ParticleRules.IsEmptiable(baseMembers[next]))
            {
                return Fail(
                    XsdRuleCodes.RcaseRecurse,
                    "rcase-Recurse.2.2",
                    r,
                    "A base particle that no particle of the group restricts is not emptiable."
                );
            }
        }

        return ParticleViolation.None;
    }

    // Choice:Choice: the members map in order to base members; base members may be left out without further condition.
    private ParticleViolation RecurseLax(
        Particle r,
        long min,
        long max,
        List<Particle> members,
        Particle b,
        List<Particle> baseMembers
    )
    {
        var range = Range(r, min, max, b, "the group");
        if (range.IsViolation)
        {
            return range;
        }

        var next = 0;
        for (var i = 0; i < members.Count; i++)
        {
            var mapped = false;
            while (next < baseMembers.Count && !mapped)
            {
                mapped = !Valid(members[i], baseMembers[next++]).IsViolation;
            }

            if (!mapped)
            {
                return Fail(
                    XsdRuleCodes.RcaseRecurseLax,
                    "rcase-RecurseLax.2",
                    members[i],
                    Describe(members[i]) + " does not restrict a base particle after the one of the previous particle."
                );
            }
        }

        return ParticleViolation.None;
    }

    // Sequence:All: every member maps to its own base member in any order, and the base members left out may be empty.
    private ParticleViolation RecurseUnordered(
        Particle r,
        List<Particle> members,
        Particle b,
        List<Particle> baseMembers
    )
    {
        var range = Range(r, r.MinOccurs, r.MaxOccurs, b, "the group");
        if (range.IsViolation)
        {
            return range;
        }

        var taken = new bool[baseMembers.Count];
        for (var i = 0; i < members.Count; i++)
        {
            var match = -1;
            var fitsTaken = false;
            for (var j = 0; match < 0 && j < baseMembers.Count; j++)
            {
                if (Valid(members[i], baseMembers[j]).IsViolation)
                {
                    continue;
                }

                if (taken[j])
                {
                    fitsTaken = true;
                }
                else
                {
                    match = j;
                }
            }

            if (match < 0)
            {
                return Fail(
                    XsdRuleCodes.RcaseRecurseUnordered,
                    fitsTaken ? "rcase-RecurseUnordered.2.1" : "rcase-RecurseUnordered.2.2",
                    members[i],
                    fitsTaken
                        ? Describe(members[i]) + " restricts a base particle that another particle already restricts."
                        : Describe(members[i]) + " does not restrict any base particle."
                );
            }

            taken[match] = true;
        }

        for (var j = 0; j < baseMembers.Count; j++)
        {
            if (!taken[j] && !ParticleRules.IsEmptiable(baseMembers[j]))
            {
                return Fail(
                    XsdRuleCodes.RcaseRecurseUnordered,
                    "rcase-RecurseUnordered.2.3",
                    r,
                    "A base particle that no particle of the sequence restricts is not emptiable."
                );
            }
        }

        return ParticleViolation.None;
    }

    // Sequence:Choice: every member restricts some base member, and the occurrences of the members add up inside the base range.
    private ParticleViolation MapAndSum(Particle r, List<Particle> members, Particle b, List<Particle> baseMembers)
    {
        for (var i = 0; i < members.Count; i++)
        {
            var found = false;
            for (var j = 0; !found && j < baseMembers.Count; j++)
            {
                found = !Valid(members[i], baseMembers[j]).IsViolation;
            }

            if (!found)
            {
                return Fail(
                    XsdRuleCodes.RcaseMapAndSum,
                    "rcase-MapAndSum.1",
                    members[i],
                    Describe(members[i]) + " does not restrict any particle of the base choice."
                );
            }
        }

        return Range(
            r,
            OccurrenceRange.Multiply(r.MinOccurs, members.Count),
            OccurrenceRange.Multiply(r.MaxOccurs, members.Count),
            b,
            "the sequence repeated for each member"
        );
    }
}
