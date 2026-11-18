namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>A variadic fake in the shape of <c>fn:concat</c>: two declared parameters, the last one repeats up to any arity.</summary>
internal sealed class FakeConcatFunction : XPathFunction
{
    public FakeConcatFunction()
        : base(
            new XPathQName(FakeBindingFunction.Namespace, "concat"),
            2,
            int.MaxValue,
            [
                new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne),
            ]
        ) { }

    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = new StringBuilder();
        for (var i = 0; i < invocation.Arguments.Length; i++)
        {
            var argument = invocation.Arguments[i];
            if (argument.Count == 0)
            {
                continue;
            }

            var item = argument[0];
            if (item.TryGetString(out var value))
            {
                _ = text.Append(value);
            }
            else if (item.TryGetInteger(out var number))
            {
                _ = text.Append(number);
            }
            else if (item.TryGetUntypedAtomic(out var untyped))
            {
                _ = text.Append(untyped);
            }
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromString(text.ToString()));
        return XPathEvaluationStatus.Ok;
    }
}
