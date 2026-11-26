namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Conformance.Results;

/// <summary>The selection of the pinned XSpec project and the results of running it.</summary>
/// <param name="Root">The root directory of the fetched project.</param>
/// <param name="Selection">The selection.</param>
/// <param name="Results">One result per expectation and per excluded part.</param>
internal sealed record XspecRun(string Root, XspecSelection Selection, IReadOnlyList<CaseResult> Results);
