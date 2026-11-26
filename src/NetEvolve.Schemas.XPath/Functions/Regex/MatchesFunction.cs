namespace NetEvolve.Schemas.XPath.Functions.Regex;

using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:matches</c> (Functions and Operators 1.0, 7.6.2): whether the input contains a match of the pattern; a pattern that matches the zero-length string is allowed.</summary>
internal sealed class MatchesFunction : RegexFunction
{
    /// <summary>Initializes a new instance of the <see cref="MatchesFunction"/> class.</summary>
    /// <param name="cache">The cache of the compiled patterns.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
    public MatchesFunction(PatternCache cache)
        : base("matches", 2, 3, [OptionalString, OneString, OneString], cache) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var operations = Compile(invocation.Arguments, 2);
        var input = Text(invocation.Arguments[0]);
        var result = operations.IsMatch(input.AsSpan());

        // A search that reached its ceiling before it found a match has no answer; one that found a match is true.
        if (result == RegexMatchResult.Aborted)
        {
            throw RegexErrors.StepCeiling();
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(result == RegexMatchResult.Match));
        return XPathEvaluationStatus.Ok;
    }
}
