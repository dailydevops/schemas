namespace NetEvolve.Schemas.XPath.Functions.Arrays;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.FunctionItems;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The bodies of the functions of the <c>array</c> namespace (Functions and Operators 3.1, 17.3). Every body works on the arguments that
/// the function conversion has checked against the declared types; an array argument and a function argument are checked here.
/// </summary>
/// <remarks>
/// Limitations: <c>array:sort</c> orders by the value comparison operators of the repository, with the codepoint collation as the only
/// collation (<see cref="CollationArgument"/>); a key that is not comparable with another key is XPTY0004.
/// </remarks>
internal static class ArrayBodies
{
    /// <summary><c>array:size</c>: the number of members.</summary>
    public static XPathEvaluationStatus Size(ref XPathFunctionCall invocation) =>
        Return(ref invocation, XdmSequence.Create(XdmItem.FromInteger(ArrayAt(invocation.Arguments, 0).Size)));

    /// <summary><c>array:get</c>: the member at a position.</summary>
    /// <exception cref="XPathException">The position is outside the array (FOAY0001).</exception>
    public static XPathEvaluationStatus Get(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        return Return(ref invocation, array[Index(invocation.Arguments[1], array.Size)]);
    }

    /// <summary><c>array:put</c>: a copy with one member replaced.</summary>
    /// <exception cref="XPathException">The position is outside the array (FOAY0001).</exception>
    public static XPathEvaluationStatus Put(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var index = Index(invocation.Arguments[1], array.Size);
        return ReturnArray(ref invocation, array.Put(index, invocation.Arguments[2].Materialize()));
    }

    /// <summary><c>array:append</c>: a copy with a member added at the end.</summary>
    public static XPathEvaluationStatus Append(ref XPathFunctionCall invocation) =>
        ReturnArray(ref invocation, ArrayAt(invocation.Arguments, 0).Append(invocation.Arguments[1].Materialize()));

    /// <summary><c>array:subarray</c>: the members from a start position, up to a length.</summary>
    /// <exception cref="XPathException">The length is negative (FOAY0002), or the range is outside the array (FOAY0001).</exception>
    public static XPathEvaluationStatus Subarray(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var array = ArrayAt(arguments, 0);
        var start = arguments[1][0].GetSaturatedInteger();
        var hasLength = arguments.Length > 2;
        var length = hasLength ? arguments[2][0].GetSaturatedInteger() : 0;
        if (hasLength && length < 0)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOAY0002);
        }

        // The range ends at start + length - 1, which must not pass the last member; the comparison avoids an overflow.
        var available = (long)array.Size + 1 - start;
        if (start < 1 || start > array.Size + 1L || (hasLength && length > available))
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOAY0001);
        }

        return ReturnArray(ref invocation, array.Slice((int)start - 1, hasLength ? (int)length : (int)available));
    }

    /// <summary><c>array:remove</c>: a copy without the members at the positions.</summary>
    /// <exception cref="XPathException">A position is outside the array (FOAY0001).</exception>
    public static XPathEvaluationStatus Remove(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var positions = invocation.Arguments[1];
        if (positions.IsEmpty)
        {
            return ReturnArray(ref invocation, array);
        }

        var removed = new bool[array.Size];
        for (var i = 0; i < positions.Count; i++)
        {
            removed[Index(XdmSequence.Create(positions[i]), array.Size)] = true;
        }

        var kept = new List<XdmSequence>(array.Size);
        for (var i = 0; i < array.Size; i++)
        {
            if (!removed[i])
            {
                kept.Add(array[i]);
            }
        }

        return ReturnArray(ref invocation, XdmArray.Create([.. kept]));
    }

    /// <summary><c>array:insert-before</c>: a copy with a member inserted before a position.</summary>
    /// <exception cref="XPathException">The position is outside the array and the end (FOAY0001).</exception>
    public static XPathEvaluationStatus InsertBefore(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var index = Index(invocation.Arguments[1], array.Size + 1);
        var members = new XdmSequence[array.Size + 1];
        for (var i = 0; i < index; i++)
        {
            members[i] = array[i];
        }

        members[index] = invocation.Arguments[2].Materialize();
        for (var i = index; i < array.Size; i++)
        {
            members[i + 1] = array[i];
        }

        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:head</c>: the first member.</summary>
    /// <exception cref="XPathException">The array is empty (FOAY0001).</exception>
    public static XPathEvaluationStatus Head(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        return Return(
            ref invocation,
            array.Size > 0 ? array[0] : throw XPathErrors.Exception(XPathErrorCodes.FOAY0001)
        );
    }

    /// <summary><c>array:tail</c>: the array without its first member; it shares the storage of the source.</summary>
    /// <exception cref="XPathException">The array is empty (FOAY0001).</exception>
    public static XPathEvaluationStatus Tail(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        return ReturnArray(
            ref invocation,
            array.Size > 0 ? array.Slice(1, array.Size - 1) : throw XPathErrors.Exception(XPathErrorCodes.FOAY0001)
        );
    }

    /// <summary><c>array:reverse</c>: the members in reverse order.</summary>
    public static XPathEvaluationStatus Reverse(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var members = new XdmSequence[array.Size];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = array[members.Length - 1 - i];
        }

        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:join</c>: the members of the arrays, one after the other.</summary>
    /// <exception cref="XPathException">An item is no array (XPTY0004).</exception>
    public static XPathEvaluationStatus Join(ref XPathFunctionCall invocation)
    {
        var input = invocation.Arguments[0];
        var arrays = new XdmArray[input.Count];
        var total = 0;
        for (var i = 0; i < arrays.Length; i++)
        {
            arrays[i] = ArrayOf(input[i]);
            total += arrays[i].Size;
        }

        if (arrays.Length == 1)
        {
            return ReturnArray(ref invocation, arrays[0]);
        }

        var members = new XdmSequence[total];
        var next = 0;
        foreach (var array in arrays)
        {
            array.CopyTo(members, next);
            next += array.Size;
        }

        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:for-each</c>: the array of the results of a function over the members.</summary>
    public static XPathEvaluationStatus ForEach(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var action = FunctionAt(invocation.Arguments, 1);
        var call = new XdmSequence[1];
        var members = new XdmSequence[array.Size];
        for (var i = 0; i < members.Length; i++)
        {
            call[0] = array[i];
            members[i] = Call(action, invocation.Context, call).Materialize();
        }

        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:filter</c>: the members for which a function gives <see langword="true"/>.</summary>
    /// <exception cref="XPathException">The function does not give one <c>xs:boolean</c> (XPTY0004).</exception>
    public static XPathEvaluationStatus Filter(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var test = FunctionAt(invocation.Arguments, 1);
        var call = new XdmSequence[1];
        var kept = new List<XdmSequence>(array.Size);
        for (var i = 0; i < array.Size; i++)
        {
            call[0] = array[i];
            var verdict = Call(test, invocation.Context, call);
            if (verdict.Count != 1 || !verdict[0].TryGetBoolean(out var keep))
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
            }

            if (keep)
            {
                kept.Add(array[i]);
            }
        }

        return ReturnArray(ref invocation, XdmArray.Create([.. kept]));
    }

    /// <summary><c>array:fold-left</c>: the members folded from the first to the last with <c>f(accumulator, member)</c>.</summary>
    public static XPathEvaluationStatus FoldLeft(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var accumulator = invocation.Arguments[1];
        var function = FunctionAt(invocation.Arguments, 2);
        var call = new XdmSequence[2];
        for (var i = 0; i < array.Size; i++)
        {
            call[0] = accumulator;
            call[1] = array[i];
            accumulator = Call(function, invocation.Context, call);
        }

        return Return(ref invocation, accumulator);
    }

    /// <summary><c>array:fold-right</c>: the members folded from the last to the first with <c>f(member, accumulator)</c>.</summary>
    public static XPathEvaluationStatus FoldRight(ref XPathFunctionCall invocation)
    {
        var array = ArrayAt(invocation.Arguments, 0);
        var accumulator = invocation.Arguments[1];
        var function = FunctionAt(invocation.Arguments, 2);
        var call = new XdmSequence[2];
        for (var i = array.Size - 1; i >= 0; i--)
        {
            call[0] = array[i];
            call[1] = accumulator;
            accumulator = Call(function, invocation.Context, call);
        }

        return Return(ref invocation, accumulator);
    }

    /// <summary><c>array:for-each-pair</c>: the array of the results of a function over the members of two arrays, as far as the shorter one goes.</summary>
    public static XPathEvaluationStatus ForEachPair(ref XPathFunctionCall invocation)
    {
        var first = ArrayAt(invocation.Arguments, 0);
        var second = ArrayAt(invocation.Arguments, 1);
        var action = FunctionAt(invocation.Arguments, 2);
        var call = new XdmSequence[2];
        var members = new XdmSequence[Math.Min(first.Size, second.Size)];
        for (var i = 0; i < members.Length; i++)
        {
            call[0] = first[i];
            call[1] = second[i];
            members[i] = Call(action, invocation.Context, call).Materialize();
        }

        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:sort</c>: the members in ascending order of their keys; the sort is stable and the source is unchanged.</summary>
    /// <exception cref="XPathException">The collation is not the codepoint collation (FOCH0002), or two keys cannot be compared (XPTY0004).</exception>
    public static XPathEvaluationStatus Sort(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var array = ArrayAt(arguments, 0);
        if (arguments.Length > 1 && !arguments[1].IsEmpty)
        {
            CollationArgument.Check(arguments, 1);
        }

        var key = arguments.Length > 2 ? FunctionAt(arguments, 2) : null;
        var call = new XdmSequence[1];
        var keys = new XdmSequence[array.Size];
        for (var i = 0; i < keys.Length; i++)
        {
            call[0] = array[i];
            keys[i] = XPathAtomization.Atomize(key is null ? array[i] : Call(key, invocation.Context, call));
        }

        var order = new int[keys.Length];
        for (var i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }

        MergeSort(order, new int[order.Length], 0, order.Length, new KeyComparer(keys, invocation.Context));
        var members = new XdmSequence[keys.Length];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = array[order[i]];
        }
        return ReturnArray(ref invocation, XdmArray.Create(members));
    }

    /// <summary><c>array:flatten</c>: the items of the input with every array replaced, recursively, by its members.</summary>
    public static XPathEvaluationStatus Flatten(ref XPathFunctionCall invocation)
    {
        var items = new List<XdmItem>();
        Flatten(invocation.Arguments[0], items);
        return Return(ref invocation, XdmSequence.Create(items.ToArray()));
    }

    // A stable merge sort of the positions: equal keys keep their order, which array:sort requires and Array.Sort does not give.
    private static void MergeSort(int[] values, int[] scratch, int start, int end, IComparer<int> comparer)
    {
        if (end - start < 2)
        {
            return;
        }

        var middle = start + ((end - start) / 2);
        MergeSort(values, scratch, start, middle, comparer);
        MergeSort(values, scratch, middle, end, comparer);
        int left = start,
            right = middle,
            next = start;
        while (left < middle && right < end)
        {
            scratch[next++] = comparer.Compare(values[right], values[left]) < 0 ? values[right++] : values[left++];
        }

        while (left < middle)
        {
            scratch[next++] = values[left++];
        }

        while (right < end)
        {
            scratch[next++] = values[right++];
        }

        Array.Copy(scratch, start, values, start, end - start);
    }

    private static void Flatten(in XdmSequence sequence, List<XdmItem> items)
    {
        for (var i = 0; i < sequence.Count; i++)
        {
            var item = sequence[i];
            if (XdmArray.TryGet(item, out var array))
            {
                for (var member = 0; member < array!.Size; member++)
                {
                    Flatten(array[member], items);
                }
            }
            else
            {
                items.Add(item);
            }
        }
    }

    private static XPathEvaluationStatus Return(ref XPathFunctionCall invocation, XdmSequence result)
    {
        invocation.Result = result;
        return XPathEvaluationStatus.Ok;
    }

    private static XPathEvaluationStatus ReturnArray(ref XPathFunctionCall invocation, XdmArray result) =>
        Return(ref invocation, XdmSequence.Create(result.ToItem()));

    private static XdmArray ArrayAt(ReadOnlySpan<XdmSequence> arguments, int index) => ArrayOf(arguments[index][0]);

    private static XdmArray ArrayOf(in XdmItem item) =>
        XdmArray.TryGet(item, out var array) ? array! : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    private static XdmFunction FunctionAt(ReadOnlySpan<XdmSequence> arguments, int index) =>
        arguments[index][0].TryGetFunction(out var function)
            ? function!
            : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

    private static XdmSequence Call(XdmFunction function, XPathEvaluationContext context, XdmSequence[] arguments) =>
        FunctionInvoker.Call(function, context, arguments, default);

    // The zero-based index of a one-based position, which must lie in 1 to the upper bound.
    private static int Index(in XdmSequence position, int upper)
    {
        var value = position[0].GetSaturatedInteger();
        return value >= 1 && value <= upper ? (int)value - 1 : throw XPathErrors.Exception(XPathErrorCodes.FOAY0001);
    }

    // Orders two keys, which are sequences of atomic values: item by item, and a shorter key before a longer one that starts with it.
    private sealed class KeyComparer(XdmSequence[] keys, XPathEvaluationContext context) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            var left = keys[x];
            var right = keys[y];
            var shared = Math.Min(left.Count, right.Count);
            for (var i = 0; i < shared; i++)
            {
                var order = CompareItems(left[i], right[i]);
                if (order != 0)
                {
                    return order;
                }
            }

            return left.Count.CompareTo(right.Count);
        }

        // NaN is less than every other value and equal to itself (Functions and Operators 3.1, 14.3.2, fn:sort).
        private int CompareItems(XdmItem left, XdmItem right)
        {
            var leftNaN = IsNaN(left);
            var rightNaN = IsNaN(right);
            if (leftNaN || rightNaN)
            {
                return leftNaN == rightNaN ? 0
                    : leftNaN ? -1
                    : 1;
            }

            left = left.TryGetUntypedAtomic(out var leftText) ? XdmItem.FromString(leftText!) : left;
            right = right.TryGetUntypedAtomic(out var rightText) ? XdmItem.FromString(rightText!) : right;
            return Holds(ComparisonRelation.LessThan, left, right) ? -1
                : Holds(ComparisonRelation.GreaterThan, left, right) ? 1
                : 0;
        }

        private bool Holds(ComparisonRelation relation, in XdmItem left, in XdmItem right) =>
            XPathEngine.Comparison.TryGetContextual(relation, left.TypeId, right.TypeId, out var implementation)
                ? implementation!(relation, left, right, context)
                : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);

        private static bool IsNaN(in XdmItem item) =>
            (item.TryGetDouble(out var wide) && double.IsNaN(wide))
            || (item.TryGetFloat(out var narrow) && float.IsNaN(narrow));
    }
}
