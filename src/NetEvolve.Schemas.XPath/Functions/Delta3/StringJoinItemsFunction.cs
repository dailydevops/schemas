namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The one-argument <c>fn:string-join</c> (Functions and Operators 3.1, 5.4.2): the strings of the argument concatenated without a
/// separator; the zero-length string for the empty sequence. The two-argument form of XPath 2.0 stays with its own function.
/// </summary>
internal sealed class StringJoinItemsFunction : Delta3Function
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="StringJoinItemsFunction"/> class.</summary>
    public StringJoinItemsFunction()
        : base("string-join", 1, 1, [StringItems]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var strings = invocation.Arguments[0];
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, invocation.Context.Limits);
        try
        {
            for (var i = 0; i < strings.Count; i++)
            {
                _ = strings[i].TryGetString(out var text);
                builder.Append(text.AsSpan());
            }

            invocation.Result = XdmSequence.Create(XdmItem.FromString(builder.ToString()));
            return XPathEvaluationStatus.Ok;
        }
        finally
        {
            builder.Dispose();
        }
    }
}
