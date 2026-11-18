namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:deep-equal</c> (Functions and Operators 1.0, 15.3.1): whether two sequences have the same length and equal items in turn.
/// Atomic items are equal by <see cref="AtomicEquality"/> with NaN equal to NaN, nodes by <see cref="NodeEquality"/>, and a node is
/// never equal to an atomic value; the arguments are not atomized.
/// </summary>
internal sealed class DeepEqualFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="DeepEqualFunction"/> class.</summary>
    public DeepEqualFunction()
        : base("deep-equal", 2, 3, [Items, Items, Collation]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The collation is not the codepoint collation (FOCH0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 2);

        var first = invocation.Arguments[0];
        var second = invocation.Arguments[1];
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(SequencesEqual(in first, in second)));
        return XPathEvaluationStatus.Ok;
    }

    private static bool SequencesEqual(in XdmSequence first, in XdmSequence second)
    {
        var count = first.Count;
        if (count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!ItemsEqual(first[i], second[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ItemsEqual(in XdmItem first, in XdmItem second)
    {
        if (first.TryGetNode(out var a))
        {
            return second.TryGetNode(out var b) && NodeEquality.DeepEqual(in a, in b);
        }

        return !second.IsNode && AtomicEquality.AreEqual(in first, in second, nanEqual: true);
    }
}
