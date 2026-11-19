namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:string-join</c> (Functions and Operators 1.0, 7.2.2): the strings of the first argument joined with the separator of the second.
/// </summary>
/// <remarks>Limitation: the one-argument form belongs to XPath 3.0 and is left to XP-71 (#334).</remarks>
internal sealed class StringJoinFunction : TextFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="StringJoinFunction"/> class.</summary>
    public StringJoinFunction()
        : base("string-join", 2, 2, [StringItems, OneString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var strings = invocation.Arguments[0];
        if (strings.Count < 2)
        {
            // Nothing to separate: the empty sequence gives the zero-length string, one string stays as it is.
            Return(ref invocation, Text(in strings));
            return XPathEvaluationStatus.Ok;
        }

        var separator = Text(invocation.Arguments[1]);
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, invocation.Context.Limits);
        try
        {
            for (var i = 0; i < strings.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator.AsSpan());
                }

                _ = strings[i].TryGetString(out var text);
                builder.Append(text.AsSpan());
            }

            Return(ref invocation, builder.ToString());
            return XPathEvaluationStatus.Ok;
        }
        finally
        {
            builder.Dispose();
        }
    }
}
