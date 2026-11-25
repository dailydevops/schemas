namespace NetEvolve.Schemas.XPath.Functions.Regex;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:tokenize</c> (Functions and Operators 1.0, 7.6.4): the parts of the input between the matches of the pattern. A match at the
/// start of the input gives an empty first token, a match at the end an empty last token, and adjacent matches an empty token between
/// them; the empty input, and the empty sequence, give the empty sequence. A pattern that matches the zero-length string is FORX0003,
/// also for the empty input. This function has the two- and three-argument forms of XPath 2.0; the one-argument form is a function of its own in <c>Functions/Delta3</c>.
/// </summary>
/// <remarks>The tokens are read from the bounds of the matches, not from the non-matching segments, which leave the empty parts out. The number of tokens is bounded by the sequence size limit of the context.</remarks>
internal sealed class TokenizeFunction : RegexFunction
{
    /// <summary>Initializes a new instance of the <see cref="TokenizeFunction"/> class.</summary>
    /// <param name="cache">The cache of the compiled patterns.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
    public TokenizeFunction(PatternCache cache)
        : base("tokenize", 2, 3, [OptionalString, OneString, OneString], cache) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var operations = Compile(invocation.Arguments, 2);
        if (operations.MatchesEmptyString)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORX0003);
        }

        var input = Text(invocation.Arguments[0]);
        if (input.Length == 0)
        {
            return XPathEvaluationStatus.Ok;
        }

        var items = new XdmItemList(invocation.Context.Limits.MaxSequenceSize, new SourceSpan(-1, 0));
        var segments = new MatchSegmentEnumerator(operations, input.AsSpan());
        try
        {
            var start = 0;
            while (segments.MoveNext())
            {
                var segment = segments.Current;
                if (segment.Kind == MatchSegmentKind.Match)
                {
                    items.Add(XdmItem.FromString(input.Substring(start, segment.Index - start)));
                    start = segment.Index + segment.Length;
                }
            }

            if (segments.IsAborted)
            {
                throw RegexErrors.StepCeiling();
            }

            items.Add(XdmItem.FromString(input.Substring(start)));
            invocation.Result = items.ToSequence();
        }
        finally
        {
            segments.Dispose();
            items.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
