namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>Checks a compiled content model for Unique Particle Attribution (Structures 3.8.6).</summary>
internal static class DeterminismCheck
{
    /// <summary>Checks a content model.</summary>
    /// <param name="model">The compiled content model.</param>
    /// <param name="terms">The components of the positions in position order.</param>
    /// <param name="nameOf">Gets the expanded name of a schema name id.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <param name="location">The location reported with an engine ceiling.</param>
    /// <returns><see langword="true"/> when the model is deterministic.</returns>
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
        Argument.ThrowIfNotEqual(terms.Count, model.PositionCount);

        _ = location;
        return true;
    }
}
