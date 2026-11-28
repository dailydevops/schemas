namespace NetEvolve.Schemas.XPath.Functions.Maps;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Arrays;
using NetEvolve.Schemas.XPath.Xdm.Maps;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>map:find</c> (Functions and Operators 3.1, 17.1.7): an array whose members are the values of every entry with the key, found by
/// descending into the maps and arrays of the input and into the values of every map.
/// </summary>
/// <remarks>Limitation: the entries of one map are visited in the order of the map (<see cref="XdmMap.Entries"/>), which the specification leaves to the implementation.</remarks>
internal sealed class MapFindFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapFindFunction"/> class.</summary>
    public MapFindFunction()
        : base("find", AnyItems, OneKey) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var members = new List<XdmSequence>();
        Find(invocation.Arguments[0], invocation.Arguments[1][0], members);
        invocation.Result = XdmSequence.Create(XdmArray.Create(members.ToArray()).ToItem());
        return XPathEvaluationStatus.Ok;
    }

    private static void Find(in XdmSequence input, in XdmItem key, List<XdmSequence> members)
    {
        for (var i = 0; i < input.Count; i++)
        {
            var item = input[i];
            if (XdmArray.TryGet(item, out var array))
            {
                for (var member = 0; member < array!.Size; member++)
                {
                    Find(array[member], key, members);
                }
            }
            else if (item.TryGetFunction(out var function) && function is XdmMap map)
            {
                if (map.TryGet(in key, out var found))
                {
                    members.Add(found);
                }

                foreach (var entry in map.Entries())
                {
                    Find(entry.Value, key, members);
                }
            }
        }
    }
}
