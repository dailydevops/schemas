namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Particles10;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

/// <summary>
/// Content type restricts (Complex Content) (<c>cos-content-act-restrict</c>, Structures 1.1, 3.4.6.4): whether every sequence of
/// element information items that the content particle of a restriction accepts is accepted by the content particle of its base
/// (clause 1), and whether the base binds each element of the sequence as generally as the restriction does (clause 2, the
/// definition of subsuming default bindings).
/// </summary>
/// <remarks>
/// <para>
/// The check is a pure function of immutable components and keeps no state, so one instance serves any number of calls. It is not
/// yet part of the content check of a derived type (clause 2.4.2 of <c>derivation-ok-restriction</c>); the pipeline of XSD-78
/// composes it, and a caller passes the content particles of both types. The inclusion of the two content models is decided on
/// their counting automata with the values of the counters kept as bounds (see <see cref="SubsumptionSearch"/> and
/// <see cref="CounterZone"/>), so an occurrence bound of one billion costs what a bound of two costs. A violation of clause 1 is
/// located at the particle of the restriction that is not covered, with the clause <c>cos-content-act-restrict.1</c>, and a
/// violation of clause 2 at the particle whose element declaration does not correspond, with a clause of the form
/// <c>cos-content-act-restrict.2.subsumes.4.1</c> that carries the numbers of the definition of subsumption.
/// </para>
/// <para>
/// A restriction and a base that are not compared give the verdict <see cref="SubsumptionVerdict.NotCompared"/>, as for the
/// restriction of XSD 1.0: a term that is not resolved (the resolver reports it), a circular group definition
/// (<c>mg-props-correct.2</c>), and an <c>all</c> group on either side, which the standard allows an implementation to accept
/// provisionally. A check that exceeds its work limit or a ceiling of the content model (more positions or transitions than the
/// automaton compiler accepts, more than <see cref="SubsumptionSearch.MaxVariables"/> counters, or an occurrence bound of 2^60 or
/// more), or finds a violation only in a zone that is not exact (see <see cref="SubsumptionSearch"/>), gives
/// <see cref="SubsumptionVerdict.Undecided"/>, and a report of it is an error with the clause
/// <c>cos-content-act-restrict.ceiling</c>, as the other analyses of content models fail when they exceed a ceiling.
/// </para>
/// <para>
/// Limitations: an element that a wildcard of the restriction matches is bound by the keyword of the wildcard, although it has the
/// global element declaration of its name as governing declaration when one exists, which the content model does not know (a
/// global element declaration of the restriction is its own governing declaration for a wildcard of the base); the
/// names that <c>##defined</c> refers to and that neither model declares are not told apart; an element declaration is compared
/// as itself, not as the substitution group it heads (#817); identity constraints (clause 4.3, #818) and type tables (clause 4.6,
/// #319) are not compared yet because the element declaration carries neither; the fixed values of clause 4.2 are compared
/// by their lexical form; both models are read with the priority of an element declaration over a wildcard, see <see cref="SubsumptionSearch"/>, and
/// the counters of a loop that are not run in step are decided only up to <see cref="SubsumptionSearch.WidenAfter"/> values per
/// pair of positions; occurrence bounds above <see cref="long.MaxValue"/> are
/// read as unbounded (<see cref="Particle"/>). The recursion over nested groups is bounded by the depth of the schema documents,
/// and the number of positions by the ceiling of the automaton compiler, which a group definition that is used many times
/// reaches before it is expanded further.
/// </para>
/// </remarks>
internal sealed class ContentSubsumption
{
    /// <summary>The work limit of one check: expansions of states and moves of the restriction tried in them.</summary>
    internal const int DefaultMaxWork = 200_000;

    // A namespace that no schema uses stands for the namespaces that neither model names.
    private const string RemainderNamespace = "urn:x-schemas:subsumption:remainder";

    // An NCName never contains an asterisk, so no declared name has this local name.
    private const string RemainderLocalName = "*remainder*";

    private readonly TypeDerivationOk _derivation;
    private readonly int _maxWork;

    /// <summary>Initializes a new instance of the <see cref="ContentSubsumption"/> class.</summary>
    /// <param name="derivation">Type Derivation OK of XSD 1.1, for the types of element declarations.</param>
    /// <param name="maxWork">The work limit of one check.</param>
    internal ContentSubsumption(TypeDerivationOk derivation, int maxWork = DefaultMaxWork)
    {
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWork, 1);

        _derivation = derivation;
        _maxWork = maxWork;
    }

    /// <summary>Checks a content particle against the content particle it restricts.</summary>
    /// <param name="restriction">The content particle of the restriction.</param>
    /// <param name="baseParticle">The content particle of the base.</param>
    /// <returns>The verdict, with the first violation in the order of the exploration for a verdict that has one.</returns>
    internal SubsumptionOutcome Check(Particle restriction, Particle baseParticle)
    {
        ArgumentNullException.ThrowIfNull(restriction);
        ArgumentNullException.ThrowIfNull(baseParticle);

        // The expansion needs finite structures; the resolver reports a circular group definition.
        if (ParticleStructure.IsCircular(restriction) || ParticleStructure.IsCircular(baseParticle))
        {
            return new(SubsumptionVerdict.NotCompared, ParticleViolation.None, 0);
        }

        var ids = new Dictionary<XPathQName, int>();
        var names = new List<XPathQName>();
        var side = SubsumptionSide.Build(restriction, ids, names);
        var baseSide = SubsumptionSide.Build(baseParticle, ids, names);
        if (side.Status == SideStatus.NotComparable || baseSide.Status == SideStatus.NotComparable)
        {
            return new(SubsumptionVerdict.NotCompared, ParticleViolation.None, 0);
        }

        if (
            side.Status != SideStatus.Ready
            || baseSide.Status != SideStatus.Ready
            || side.Caps.Length + baseSide.Caps.Length > SubsumptionSearch.MaxVariables
        )
        {
            return Undecided(restriction, 0);
        }

        var search = new SubsumptionSearch(
            side,
            baseSide,
            SymbolsOf(names, side, baseSide),
            (position, basePosition) =>
                Correspond(side.Particles[position], side.Terms[position], baseSide.Terms[basePosition]),
            _maxWork
        );
        var (violation, exceeded) = search.Run();
        if (exceeded)
        {
            return Undecided(restriction, search.Work);
        }

        return violation.IsViolation
            ? new(SubsumptionVerdict.NotSubsumed, violation, search.Work)
            : new(SubsumptionVerdict.Subsumed, ParticleViolation.None, search.Work);
    }

    /// <summary>Checks a content particle against the particle it restricts and reports a violation as a located issue.</summary>
    /// <param name="restriction">The content particle of the restriction.</param>
    /// <param name="baseParticle">The content particle of the base.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(Particle restriction, Particle baseParticle, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var violation = Check(restriction, baseParticle).Violation;
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

    private static SubsumptionOutcome Undecided(Particle restriction, int work) =>
        new(
            SubsumptionVerdict.Undecided,
            new ParticleViolation(
                XsdRuleCodes.CosContentActRestrict,
                "cos-content-act-restrict.ceiling",
                "The content model of the restriction could not be compared with that of its base within the ceilings of the check.",
                restriction.Location
            ),
            work
        );

    // The element names of both models, the names a wildcard disallows, and one name of each namespace for all other names.
    private static XPathQName[] SymbolsOf(List<XPathQName> names, SubsumptionSide side, SubsumptionSide baseSide)
    {
        var symbols = new List<XPathQName>(names);
        var known = new HashSet<XPathQName>(names);
        var namespaces = new SortedSet<string>(StringComparer.Ordinal) { string.Empty, RemainderNamespace };
        foreach (var name in names)
        {
            _ = namespaces.Add(name.NamespaceName);
        }

        foreach (var terms in new[] { side.Terms, baseSide.Terms })
        {
            foreach (var term in terms)
            {
                if (term is not IWildcard wildcard)
                {
                    continue;
                }

                var constraint = NegatedWildcard.ConstraintOf(wildcard);
                foreach (var space in constraint.Namespaces)
                {
                    _ = namespaces.Add(space);
                }

                foreach (var disallowed in constraint.DisallowedNames)
                {
                    if (known.Add(disallowed))
                    {
                        symbols.Add(disallowed);
                    }
                }
            }
        }

        foreach (var space in namespaces)
        {
            symbols.Add(new XPathQName(space, RemainderLocalName));
        }

        return symbols.ToArray();
    }

    private static ParticleViolation Fail(string clause, Particle at, string message) =>
        new(XsdRuleCodes.CosContentActRestrict, "cos-content-act-restrict.2.subsumes." + clause, message, at.Location);

    private static IXsdComponent? TypeOf(ElementDeclaration element) =>
        element.TypeDefinition
        ?? (element.InlineTypeSyntax is null && element.ReferenceCount == 0 ? ComplexTypeDefinition.AnyType : null);

    // The default binding G of the base subsumes the default binding S of the restriction (clause 2 and the definition of subsumption).
    private ParticleViolation Correspond(Particle at, IXsdComponent specific, IXsdComponent general)
    {
        if (ReferenceEquals(specific, general))
        {
            return ParticleViolation.None;
        }

        switch (specific, general)
        {
            case (ElementDeclaration element, ElementDeclaration baseElement):
                return CorrespondElements(at, element, baseElement);
            case (ElementDeclaration element, IWildcard baseWildcard):
                // Clauses 1 and 2: skip and lax subsume an element declaration, strict does not (clause 3 needs both strict);
                // a global declaration is the governing declaration of its name for the wildcard too, so it subsumes itself.
                return baseWildcard.ProcessContents == XsdProcessContents.Strict && !element.IsGlobal
                    ? Fail("3", at, "A strict wildcard of the base is bound more strictly than an element declaration.")
                    : ParticleViolation.None;
            case (IWildcard wildcard, IWildcard baseWildcard):
                // Strict, lax, skip are ordered from the strongest to the weakest binding; the base may only be weaker.
                return wildcard.ProcessContents > baseWildcard.ProcessContents
                    ? Fail(
                        baseWildcard.ProcessContents == XsdProcessContents.Strict ? "3" : "2",
                        at,
                        "The process contents of the wildcard must be identical to or stronger than that of the wildcard of the base."
                    )
                    : ParticleViolation.None;
            default:
                // A wildcard of the restriction is bound by a keyword, which no element declaration of the base subsumes (clause 4).
                return Fail(
                    "4",
                    at,
                    "A wildcard of the restriction cannot restrict an element declaration of the base."
                );
        }
    }

    private ParticleViolation CorrespondElements(
        Particle at,
        ElementDeclaration element,
        ElementDeclaration baseElement
    )
    {
        var name = element.Name.LocalName;
        if (element.IsNillable && !baseElement.IsNillable)
        {
            return Fail("4.1", at, "The element '" + name + "' is nillable but the element of the base is not.");
        }

        if (
            baseElement.ValueConstraint.Kind == ElementValueConstraintKind.Fixed
            && (
                element.ValueConstraint.Kind != ElementValueConstraintKind.Fixed
                || !string.Equals(
                    element.ValueConstraint.Value,
                    baseElement.ValueConstraint.Value,
                    StringComparison.Ordinal
                )
            )
        )
        {
            return Fail(
                "4.2",
                at,
                "The element '" + name + "' is fixed in the base and must keep the same fixed value."
            );
        }

        if ((baseElement.Block & ~element.Block) != XsdDerivationMethod.None)
        {
            return Fail(
                "4.4",
                at,
                "The disallowed substitutions of the element '"
                    + name
                    + "' must include those of the element of the base."
            );
        }

        // Clause 4.5: validly substitutable as a restriction, so a step by extension is blocked.
        if (TypeOf(element) is not { } type || TypeOf(baseElement) is not { } baseType)
        {
            return ParticleViolation.None;
        }

        var violation = _derivation.Check(type, baseType, XsdDerivationMethod.Extension);
        return violation.IsViolation
            ? Fail(
                "4.5",
                at,
                "The type of the element '"
                    + name
                    + "' must be derived by restriction from the type of the element of the base: "
                    + violation.Message
            )
            : ParticleViolation.None;
    }
}
