namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:matches</c>, <c>fn:replace</c>, and <c>fn:tokenize</c> (Functions and Operators 1.0, 7.6): the input, which an
/// empty sequence makes the zero-length string, the pattern, and the flags, which the shared <see cref="PatternCache"/> compiles. The
/// functions read the input through the <see cref="MatchSegmentEnumerator"/>.
/// </summary>
/// <remarks>
/// Limitations: the dialect is that of the XPath regular expressions of the Xml package; the flag <c>q</c> of the later editions is an
/// invalid flag (FORX0001), and a step ceiling of the regex engine ends a function with the engine code of
/// <see cref="RegexErrors.LimitExceeded"/>, although the specification defines an answer.
/// </remarks>
internal abstract class RegexFunction : TextFunction
{
    private readonly PatternCache _cache;

    /// <summary>Initializes a new instance of the <see cref="RegexFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <param name="cache">The cache of the compiled patterns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/>, <paramref name="parameters"/>, or <paramref name="cache"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected RegexFunction(
        string localName,
        int minArity,
        int maxArity,
        IReadOnlyList<XPathParameterType> parameters,
        PatternCache cache
    )
        : base(localName, minArity, maxArity, parameters)
    {
        Argument.ThrowIfNull(cache);

        _cache = cache;
    }

    /// <summary>Gets the operations of the pattern of a call: the second argument, with the flags at <paramref name="flagsIndex"/> when the call has them.</summary>
    /// <param name="arguments">The converted arguments of the call.</param>
    /// <param name="flagsIndex">The position of the flags argument.</param>
    /// <returns>The operations.</returns>
    /// <exception cref="XPathException">A flag is invalid (FORX0001), the pattern is invalid (FORX0002), or it exceeds a limit of the regex engine.</exception>
    protected RegexOperations Compile(ReadOnlySpan<XdmSequence> arguments, int flagsIndex) =>
        _cache.Get(Text(arguments[1]), flagsIndex < arguments.Length ? Text(arguments[flagsIndex]) : string.Empty);
}
