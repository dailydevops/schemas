namespace NetEvolve.Schemas.Xml.Regex.Contracts;

using System;

/// <summary>A compiled pattern that matches spans. The parser and the matcher are DT-11 and DT-12; this contract only fixes the signature.</summary>
public interface IRegexMatcher
{
    /// <summary>Gets the dialect the pattern was compiled for: whole-input match for <see cref="RegexDialect.Xsd"/>, search for <see cref="RegexDialect.XPath"/>.</summary>
    RegexDialect Dialect { get; }

    /// <summary>Matches an input.</summary>
    /// <param name="input">The input.</param>
    /// <param name="limits">The step ceilings.</param>
    /// <returns>The outcome; <see cref="RegexMatchResult.Aborted"/> when a step ceiling was reached. The matcher never throws for input problems.</returns>
    RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits);
}
