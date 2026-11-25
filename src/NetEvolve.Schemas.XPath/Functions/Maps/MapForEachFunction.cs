namespace NetEvolve.Schemas.XPath.Functions.Maps;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.FunctionItems;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>map:for-each</c> (Functions and Operators 3.1, 17.1.11): the concatenation of the results of calling a function with the key and the
/// value of each entry, in no specified order.
/// </summary>
/// <remarks>Limitation: the action must be a function item of arity 2; a function item of another arity is XPTY0004 and is not coerced.</remarks>
internal sealed class MapForEachFunction : MapFunction
{
    /// <summary>Initializes a new instance of the <see cref="MapForEachFunction"/> class.</summary>
    public MapForEachFunction()
        : base("for-each", OneItem, OneItem) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var map = RequireMap(invocation.Arguments[0]);
        var action = invocation.Arguments[1];
        if (action.Count != 1 || !action[0].TryGetFunction(out var function) || function!.Arity != 2)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        var results = new List<XdmItem>();
        foreach (var entry in map.Entries())
        {
            var arguments = new[] { XdmSequence.Create(entry.Key), entry.Value };
            foreach (var item in FunctionInvoker.Call(function, invocation.Context, arguments, default(SourceSpan)))
            {
                results.Add(item);
            }
        }

        invocation.Result = XdmSequence.Create(results.ToArray());
        return XPathEvaluationStatus.Ok;
    }
}
