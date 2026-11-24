namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:trace</c> (Functions and Operators 1.0, 3.2.1): the value of the first argument, unchanged. The label goes to the trace slot
/// of the host (<see cref="Abstractions.XPathHostSlots"/>) when it is set, and nowhere otherwise; the destination is
/// implementation-dependent.
/// </summary>
internal sealed class TraceFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="TraceFunction"/> class.</summary>
    public TraceFunction()
        : base("trace", 2, 2, [Items, Collation]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var value = invocation.Arguments[0];
        _ = invocation.Arguments[1][0].TryGetString(out var label);
        invocation.Context.Host.Trace?.Invoke(label!, value);
        invocation.Result = value;
        return XPathEvaluationStatus.Ok;
    }
}
