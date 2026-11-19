namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:insert-before</c> (Functions and Operators 1.0, 15.1.2): the target with the inserts placed before the item at the position.
/// A position below one inserts at the start, and a position beyond the end appends.
/// </summary>
internal sealed class InsertBeforeFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="InsertBeforeFunction"/> class.</summary>
    public InsertBeforeFunction()
        : base("insert-before", 3, 3, [Items, OneInteger, Items]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var target = invocation.Arguments[0];
        var inserts = invocation.Arguments[2];
        if (!inserts.HasItems())
        {
            invocation.Result = target;
            return XPathEvaluationStatus.Ok;
        }

        var count = target.Count;
        if (count == 0)
        {
            invocation.Result = inserts;
            return XPathEvaluationStatus.Ok;
        }

        var position = invocation.Arguments[1][0].GetSaturatedInteger();
        var at =
            position < 1 ? 0
            : position > count ? count
            : (int)position - 1;
        var result = default(XdmItemList);
        try
        {
            for (var i = 0; i < at; i++)
            {
                result.Add(target[i]);
            }

            result.AddRange(inserts);
            for (var i = at; i < count; i++)
            {
                result.Add(target[i]);
            }

            invocation.Result = result.ToSequence();
        }
        finally
        {
            result.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
