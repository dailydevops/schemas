namespace NetEvolve.Schemas.Tests.Architecture.XPath;

/// <summary>A named layering rule; <paramref name="Find"/> returns the offending type names or folders.</summary>
internal sealed record XPathLayerRule(string Name, Func<XPathLayerInput, IReadOnlyList<string>> Find);
