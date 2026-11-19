namespace NetEvolve.Schemas.XPath.Functions.Strings;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:string-length</c> (Functions and Operators 1.0, 7.4.4): the number of code points of the argument, 0 for the empty sequence,
/// and the number of the string value of the context item without an argument. Allocates nothing for a string argument.
/// </summary>
internal sealed class StringLengthFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="StringLengthFunction"/> class.</summary>
    public StringLengthFunction()
        : base("string-length", 0, 1, [OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = invocation.Arguments.Length == 0 ? ContextText(ref invocation) : Text(invocation.Arguments[0]);
        invocation.Result = XdmSequence.Create(XdmItem.FromInteger(CodePoints.Count(text.AsSpan())));
        return XPathEvaluationStatus.Ok;
    }
}
