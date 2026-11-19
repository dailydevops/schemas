namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// The seam of the Unique Particle Attribution check (design #146 seam table), so the schema compilation pipeline can be
/// tested with a fake.
/// </summary>
internal interface IDeterminismCheck
{
    /// <summary>Checks a compiled content model.</summary>
    /// <param name="model">The compiled content model.</param>
    /// <param name="terms">The components of the positions in position order: an element declaration or a wildcard.</param>
    /// <param name="nameOf">Gets the expanded name of a schema name id, for the wildcards that allow it.</param>
    /// <param name="reporter">Receives the schema errors.</param>
    /// <param name="location">The location reported with an engine ceiling.</param>
    /// <returns><see langword="true"/> when the model is deterministic and the analysis was complete.</returns>
    bool Check(
        CountingAutomaton model,
        IReadOnlyList<IXsdComponent> terms,
        Func<int, XPathQName> nameOf,
        XsdIssueReporter reporter,
        DocumentLocation location
    );
}
