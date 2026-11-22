namespace NetEvolve.Schemas.XPath.Functions.Regex;

using System;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// <c>fn:replace</c> (Functions and Operators 1.0, 7.6.3): every match of the pattern is replaced by the replacement string; an input
/// without a match, and the empty input, are returned as they are. A pattern that matches the zero-length string is FORX0003 (also for
/// the empty input), and a lone dollar sign or backslash in the replacement string is FORX0004, which is checked after FORX0003 and also
/// when nothing matches.
/// </summary>
/// <remarks>The result is built in a stack buffer that grows into a pooled array under the string length limit of the context, so only the result string is allocated.</remarks>
internal sealed class ReplaceFunction : RegexFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="ReplaceFunction"/> class.</summary>
    /// <param name="cache">The cache of the compiled patterns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
    public ReplaceFunction(PatternCache cache)
        : base("replace", 3, 4, [OptionalString, OneString, OneString, OneString], cache) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var operations = Compile(invocation.Arguments, 3);
        var input = Text(invocation.Arguments[0]);
        var replacement = Text(invocation.Arguments[2]);
        if (operations.MatchesEmptyString)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORX0003);
        }

        if (!ReplacementSyntax.IsValid(replacement.AsSpan()))
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORX0004);
        }

        Return(ref invocation, Replace(operations, input, replacement, invocation.Context.Limits));
        return XPathEvaluationStatus.Ok;
    }

    private static string Replace(RegexOperations operations, string input, string replacement, XPathLimits limits)
    {
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, limits);
        var segments = new MatchSegmentEnumerator(operations, input.AsSpan());
        try
        {
            var replaced = false;
            while (segments.MoveNext())
            {
                var segment = segments.Current;
                if (segment.Kind == MatchSegmentKind.Match)
                {
                    replaced = true;
                    ReplacementExpander.Append(ref builder, replacement.AsSpan(), in segment);
                }
                else
                {
                    builder.Append(segment.Text);
                }
            }

            if (segments.IsAborted)
            {
                throw RegexErrors.StepCeiling();
            }

            return replaced ? builder.ToString() : input;
        }
        finally
        {
            segments.Dispose();
            builder.Dispose();
        }
    }
}
