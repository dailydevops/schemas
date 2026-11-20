namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism11;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Wildcards.Negation;

/// <summary>
/// Checks a compiled content model for Unique Particle Attribution as XSD 1.1 defines it (Structures 3.8.6): an element
/// declaration and a wildcard that allow the same element do not compete, because the declaration wins; two element
/// declarations with one name, and two wildcards that allow a common namespace, still do. The counters are decided as
/// in <see cref="DeterminismCheck"/>, which is the check of XSD 1.0.
/// </summary>
/// <remarks>
/// <para>
/// The analysis is that of <see cref="RegionAnalysis"/>, which finds the pairs of positions that one state offers for one
/// element. It sees every wildcard through a view that allows no element, so a pair of an element and a wildcard is never
/// found, and a pair of two wildcards is found for every two that one state offers; those pairs are then kept only when the
/// namespace constraints of the two wildcards overlap. A namespace holds infinitely many names, so the disallowed names
/// and the keywords of a wildcard never make two wildcards disjoint, only the namespaces do.
/// </para>
/// <para>
/// Limitations: the wildcards are the component of XSD 1.1 (<see cref="NegatedWildcard"/>) or the wildcard of XSD 1.0, any
/// other implementation of the seam is rejected when two wildcards are compared; all groups are not automata and are not
/// checked here; the ceilings of <see cref="DeterminismCheck"/> apply and are reported as an engine issue.
/// </para>
/// </remarks>
internal static class DeterminismCheck11
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
        Argument.ThrowIfNull(model);
        Argument.ThrowIfNull(terms);
        Argument.ThrowIfNull(nameOf);
        Argument.ThrowIfNull(reporter);

        var views = new List<IXsdComponent>(terms.Count);
        for (var i = 0; i < terms.Count; i++)
        {
            Argument.ThrowIfNull(terms[i]);

            views.Add(terms[i] is IWildcard wildcard ? new ElementPriorityView(wildcard) : terms[i]);
        }

        var analysis = new RegionAnalysis(model, new ParticleOverlap(model, views, nameOf));
        var complete = analysis.Explore();

        // Position order, so the issues do not depend on the order the states were explored in.
        var pairs = analysis.Pairs;
        pairs.Sort();
        var ambiguous = 0;
        foreach (var pair in pairs)
        {
            var first = terms[(int)(pair >> 32)];
            var second = terms[(int)(uint)pair];
            if (!Competes(first, second))
            {
                continue;
            }

            ambiguous++;
            Report(reporter, first, second);
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

        return complete && ambiguous == 0;
    }

    // Two elements that were found share a name. Two wildcards were found for being offered together: they compete
    // when they allow a common namespace.
    private static bool Competes(IXsdComponent first, IXsdComponent second) =>
        first is not IWildcard left
        || second is not IWildcard right
        || NegatedWildcard.ConstraintOf(left).Overlaps(NegatedWildcard.ConstraintOf(right));

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
        term is IWildcard wildcard ? "wildcard " + NegatedWildcard.ConstraintOf(wildcard) : "element " + term.Name;
}
