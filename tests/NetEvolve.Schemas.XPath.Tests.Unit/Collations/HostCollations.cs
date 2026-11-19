namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using NetEvolve.Schemas.XPath.Collations;

/// <summary>Collations a host could register: an ordinal collation that ignores case, and one that reports a fixed magnitude.</summary>
internal static class HostCollations
{
    internal const string CaseBlindUri = "http://example.com/collations/case-blind";

    /// <summary>Compares ordinally and ignores case; the substring operations follow the same rule.</summary>
    internal sealed class CaseBlind(string uri = CaseBlindUri) : Collation(uri)
    {
        public override int Compare(string first, string second) =>
            string.Compare(first, second, StringComparison.OrdinalIgnoreCase);

        public override bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
            text.StartsWith(value, StringComparison.OrdinalIgnoreCase);

        public override bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) =>
            text.EndsWith(value, StringComparison.OrdinalIgnoreCase);

        public override bool TryFind(ReadOnlySpan<char> text, ReadOnlySpan<char> value, out int index, out int length)
        {
            index = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
            length = value.Length;
            return index >= 0;
        }
    }

    /// <summary>Answers every comparison with one number, which is not -1, 0, or 1.</summary>
    internal sealed class FixedMagnitude(string uri, int magnitude) : Collation(uri)
    {
        public override int Compare(string first, string second) => magnitude;

        public override bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) => false;

        public override bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) => false;

        public override bool TryFind(ReadOnlySpan<char> text, ReadOnlySpan<char> value, out int index, out int length)
        {
            index = -1;
            length = 0;
            return false;
        }
    }
}
