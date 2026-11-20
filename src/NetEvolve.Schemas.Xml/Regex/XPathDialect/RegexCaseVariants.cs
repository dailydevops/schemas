namespace NetEvolve.Schemas.Xml.Regex.XPathDialect;

using System;

internal static class RegexCaseVariants
{
    internal const int ScanLimit = 0x2FFFF;

    internal static bool TryGetVariants(int codePoint, out ReadOnlySpan<int> variants)
    {
        _ = codePoint;
        variants = default;
        return false;
    }

    internal static int[] Close(int[] ranges) => ranges;
}
