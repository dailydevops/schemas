namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

/// <summary>An element of a test tree; the content is a mix of <see cref="PatternAttribute"/>, <see cref="PatternElement"/>, and text.</summary>
internal sealed record PatternElement(string Name, object[] Content);
