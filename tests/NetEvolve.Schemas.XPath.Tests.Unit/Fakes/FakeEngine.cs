namespace NetEvolve.Schemas.XPath.Tests.Unit.Fakes;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A fake engine over the contract types only. It compiles <c>prefix:name(.)</c> or <c>prefix:name(@attribute)</c>,
/// resolves the function through the static context, and evaluates it against the focus of the evaluation context.
/// </summary>
internal sealed class FakeEngine
{
    private readonly XPathHostFunction _function;
    private readonly string _argument;

    private FakeEngine(XPathHostFunction function, string argument)
    {
        _function = function;
        _argument = argument;
    }

    public static FakeEngine Compile(string expression, XPathStaticContext context)
    {
        var open = expression.IndexOf('(');
        var local = expression[(expression.IndexOf(':') + 1)..open];
        var argument = expression[(open + 1)..^1];
        _ = context.TryResolveFunction(new XPathQName(FakeFunction.Namespace, local), 1, out var function);
        return new FakeEngine(function!, argument);
    }

    public XdmSequence Evaluate(XPathEvaluationContext context)
    {
        var focus = context.ContextItem!.Value;
        var argument = XdmSequence.Create(focus);
        if (_argument.Length > 1 && focus.TryGetNode(out var node))
        {
            var attribute = node.Node.FirstAttribute;
            argument = attribute.IsEmpty ? XdmSequence.Empty : XdmSequence.Create(XdmItem.FromNode(new(attribute)));
        }

        XdmSequence[] arguments = [argument];
        var call = new XPathFunctionCall(context, arguments);
        _ = _function.Invoke(ref call);
        return call.Result;
    }
}
