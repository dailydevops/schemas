namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>The split of the XSpec files into expectations that run and parts that are excluded.</summary>
/// <param name="Selected">The expectations to run.</param>
/// <param name="Excluded">The parts left out, each with a reason.</param>
internal sealed record XspecSelection(IReadOnlyList<XspecCase> Selected, IReadOnlyList<XspecExclusion> Excluded);
