namespace NetEvolve.Schemas.Tests.Architecture.XPath;

internal static class XPathLayering
{
    internal static IReadOnlyList<XPathLayerRule> Rules { get; } =
    [new("Unimplemented", _ => throw new NotSupportedException("Not implemented yet."))];
}
