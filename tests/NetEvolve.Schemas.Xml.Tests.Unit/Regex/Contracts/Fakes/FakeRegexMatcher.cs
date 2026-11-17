namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Contracts.Fakes;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>A scripted matcher: matches the literal pattern, and aborts when the input is longer than the step ceiling.</summary>
internal sealed class FakeRegexMatcher(string pattern, RegexDialect dialect) : IRegexMatcher
{
    public RegexDialect Dialect => dialect;

    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits)
    {
        if (input.Length > limits.MaxNfaSteps)
        {
            return RegexMatchResult.Aborted;
        }

        var matches =
            input.SequenceEqual(pattern.AsSpan())
            || (dialect == RegexDialect.XPath && input.IndexOf(pattern.AsSpan()) >= 0);

        return matches ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
    }
}
