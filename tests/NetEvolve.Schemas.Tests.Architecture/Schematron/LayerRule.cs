namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

/// <summary>A named layering rule; <paramref name="Find"/> returns the offending type names.</summary>
internal sealed record LayerRule(string Name, Func<LayerInput, IReadOnlyList<string>> Find);
