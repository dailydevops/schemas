namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System.Collections.Generic;

/// <summary>A constraint that the reader does not enforce yet: the issues that track it and the tests that pin the gap.</summary>
/// <param name="Issues">The tracking issues, each <c>#</c> and its number; at least one.</param>
/// <param name="Probes">
/// The references of the tests that read a violating document and assert that the run does not report it; at least one.
/// </param>
internal sealed record ConstraintGap(IReadOnlyList<string> Issues, IReadOnlyList<string> Probes);
