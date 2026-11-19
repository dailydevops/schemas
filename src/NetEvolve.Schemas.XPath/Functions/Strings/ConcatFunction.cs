namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Nodes;

/// <summary>
/// <c>fn:concat</c> (Functions and Operators 1.0, 7.2.1): the string values of two or more arguments joined; an empty sequence counts
/// as the zero-length string. The declared parameter type of the last argument repeats for every further one (variadic).
/// </summary>
/// <remarks>
/// Limitation: the string value of an argument comes from <c>AtomicString</c>, which has no value for an item without a value type yet
/// (<c>xs:QName</c>, the binary and the Gregorian types, #545), so such an argument raises XPTY0004 where the specification casts it to a
/// string; the casting table replaces <c>AtomicString</c> in #625.
/// </remarks>
internal sealed class ConcatFunction : TextFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="ConcatFunction"/> class.</summary>
    public ConcatFunction()
        : base("concat", 2, int.MaxValue, [OptionalAtomic, OptionalAtomic]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, invocation.Context.Limits);
        try
        {
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                var argument = invocation.Arguments[i];
                if (!argument.IsEmpty)
                {
                    var item = argument[0];
                    builder.Append(AtomicString.Of(in item).AsSpan());
                }
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
