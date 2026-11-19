namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>The region analysis behind <see cref="IDeterminismCheck"/>.</summary>
internal sealed class DeterminismChecker : IDeterminismCheck
{
    /// <inheritdoc />
    public bool Check(
        CountingAutomaton model,
        IReadOnlyList<IXsdComponent> terms,
        Func<int, XPathQName> nameOf,
        XsdIssueReporter reporter,
        DocumentLocation location
    ) => DeterminismCheck.Check(model, terms, nameOf, reporter, location);
}
