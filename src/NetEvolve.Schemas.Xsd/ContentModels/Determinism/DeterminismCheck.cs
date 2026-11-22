namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Wildcards.Core;

/// <summary>
/// Checks a compiled content model for Unique Particle Attribution (<c>cos-nonambig</c>, Structures 3.8.6, design #146
/// DP-8): no state of the model has two distinct particles that can match the same element. Two paths to one particle with
/// different counter values, as in <c>(a{2,3}){2}</c>, are not ambiguous. The counters are decided by classes, never by
/// unfolding, so the verdict does not depend on the size of an occurrence bound.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: this is the Structures 1.0 rule, so an element that a wildcard also allows is ambiguous; the element
/// priority of XSD 1.1 is <c>DeterminismCheck11</c> (#306). The input is the compiled <see cref="CountingAutomaton"/> and the component of every
/// position, because the automaton keeps neither locations nor expanded names; the position of a term is its index in
/// <c>terms</c>, which is the document order of the leaves with the particles that cannot occur left out, the numbering
/// of the compiler. All groups are not automata and are not checked here. The analysis stops at an internal ceiling of
/// <see cref="RegionAnalysis.MaxStates"/> states and reports it as an engine issue.
/// </para>
/// <para>
/// An issue is located at the earlier particle and names both particles with their locations, because an issue carries
/// one location.
/// </para>
/// </remarks>
internal static class DeterminismCheck
{
    /// <summary>Checks a content model.</summary>
    /// <param name="model">The compiled content model.</param>
    /// <param name="terms">The components of the positions in position order: an element declaration or a wildcard.</param>
    /// <param name="nameOf">Gets the expanded name of a schema name id, for the wildcards that allow it.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <param name="location">The location reported with an engine ceiling.</param>
    /// <returns><see langword="true"/> when the model is deterministic and the analysis was complete.</returns>
    internal static bool Check(
        CountingAutomaton model,
        IReadOnlyList<IXsdComponent> terms,
        Func<int, XPathQName> nameOf,
        XsdIssueReporter reporter,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(nameOf);
        ArgumentNullException.ThrowIfNull(reporter);

        var overlap = new ParticleOverlap(model, terms, nameOf);
        var analysis = new RegionAnalysis(model, overlap);
        var complete = analysis.Explore();

        // Position order, so the issues do not depend on the order the states were explored in.
        var pairs = analysis.Pairs;
        pairs.Sort();
        foreach (var pair in pairs)
        {
            Report(reporter, terms[(int)(pair >> 32)], terms[(int)(uint)pair]);
        }

        if (!complete)
        {
            reporter.Report(
                XsdRuleCodes.DeterminismAnalysisCeilingExceeded,
                location,
                "The unique particle attribution analysis of the content model exceeds its internal ceiling of "
                    + RegionAnalysis.MaxStates
                    + " states or "
                    + RegionAnalysis.MaxLiveCounters
                    + " counters."
            );
        }

        return complete && pairs.Count == 0;
    }

    private static void Report(XsdIssueReporter reporter, IXsdComponent first, IXsdComponent second) =>
        reporter.Report(
            XsdRuleCodes.CosNonambig,
            first.Location,
            "The content model is ambiguous: the "
                + Describe(first)
                + " at "
                + first.Location
                + " and the "
                + Describe(second)
                + " at "
                + second.Location
                + " can match the same element."
        );

    private static string Describe(IXsdComponent term) =>
        term is Wildcard wildcard ? "wildcard " + wildcard.Constraint : "element " + term.Name;
}
