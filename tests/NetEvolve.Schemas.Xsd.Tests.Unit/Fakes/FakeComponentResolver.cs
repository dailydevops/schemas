namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>A resolver that logs the number of components it sees.</summary>
internal sealed class FakeComponentResolver : IComponentResolver
{
    private readonly List<string> _log;

    public FakeComponentResolver(List<string> log) => _log = log;

    public void Resolve(IComponentRegistry registry, XsdIssueReporter reporter) =>
        _log.Add($"Resolve:{registry.Count}");
}
