namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The one-argument <c>fn:tokenize</c> (Functions and Operators 3.1, 5.6.4): the whitespace-separated tokens of the input, which is
/// <c>fn:tokenize(fn:normalize-space($input), ' ')</c>; the empty sequence for the empty sequence, the zero-length string, and a string
/// of whitespace only. The two- and three-argument forms of XPath 2.0 stay with their own function.
/// </summary>
/// <remarks>The number of tokens is bounded by the sequence size limit of the context.</remarks>
internal sealed class TokenizeWhitespaceFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="TokenizeWhitespaceFunction"/> class.</summary>
    public TokenizeWhitespaceFunction()
        : base("tokenize", 1, 1, [OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        _ = argument[0].TryGetString(out var input);
        var items = new XdmItemList(invocation.Context.Limits.MaxSequenceSize, new SourceSpan(-1, 0));
        try
        {
            var position = 0;
            while (WhitespaceTokens.Next(input.AsSpan(), ref position, out var start, out var length))
            {
                items.Add(XdmItem.FromString(input!.Substring(start, length)));
            }

            invocation.Result = items.ToSequence();
        }
        finally
        {
            items.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
