namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:index-of</c> (Functions and Operators 1.0, 15.1.3): the positions, counted from one, of the values of the first argument that
/// are equal to the second. Equality is <see cref="AtomicEquality"/> with NaN equal to nothing, so a search for NaN finds none, and
/// values that cannot be compared are skipped without an error.
/// </summary>
internal sealed class IndexOfFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="IndexOfFunction"/> class.</summary>
    public IndexOfFunction()
        : base("index-of", 2, 3, [Atomics, OneAtomic, Collation]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The collation is not the codepoint collation (FOCH0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 2);

        var items = invocation.Arguments[0];
        var search = invocation.Arguments[1][0];
        var count = items.Count;
        var positions = default(XdmItemList);
        try
        {
            for (var i = 0; i < count; i++)
            {
                if (AtomicEquality.AreEqual(items[i], search, nanEqual: false, invocation.Context))
                {
                    positions.Add(XdmItem.FromInteger(i + 1L));
                }
            }

            invocation.Result = positions.ToSequence();
        }
        finally
        {
            positions.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
