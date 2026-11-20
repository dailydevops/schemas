namespace NetEvolve.Schemas.Xsd.ContentModels.Determinism11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>The XSD 1.1 check behind <see cref="IDeterminismCheck"/>.</summary>
internal sealed class DeterminismChecker11 : IDeterminismCheck
{
    /// <inheritdoc />
    public bool Check(
        CountingAutomaton model,
        IReadOnlyList<IXsdComponent> terms,
        Func<int, XPathQName> nameOf,
        XsdIssueReporter reporter,
        DocumentLocation location
    ) => DeterminismCheck11.Check(model, terms, nameOf, reporter, location);
}
