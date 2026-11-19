namespace NetEvolve.Schemas.XPath.Functions.Strings;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:string-to-codepoints</c> (Functions and Operators 1.0, 7.2.4): the code points of the argument as integers, in order; the
/// empty sequence for the empty sequence and for the zero-length string. A code point outside the Basic Multilingual Plane is one integer.
/// </summary>
internal sealed class StringToCodepointsFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="StringToCodepointsFunction"/> class.</summary>
    public StringToCodepointsFunction()
        : base("string-to-codepoints", 1, 1, [OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = Text(invocation.Arguments[0]);
        if (text.Length == 0)
        {
            return XPathEvaluationStatus.Ok;
        }

        // The list is bounded by the sequence limit of the context and returns its pooled array when disposed.
        var items = new XdmItemList(invocation.Context.Limits.MaxSequenceSize, new SourceSpan(-1, 0));
        try
        {
            var span = text.AsSpan();
            var index = 0;
            while (index < span.Length)
            {
                items.Add(XdmItem.FromInteger(CodePoints.Next(span, ref index)));
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
