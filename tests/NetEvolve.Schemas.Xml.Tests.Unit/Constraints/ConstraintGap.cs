namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

/// <summary>A constraint that the reader does not enforce yet: the issue that tracks it and the test that pins the gap.</summary>
/// <param name="Issue">The tracking issue, <c>#</c> and its number.</param>
/// <param name="Probe">The reference of the test that reads a violating document and asserts that the run does not report it.</param>
internal sealed record ConstraintGap(string Issue, string Probe);
