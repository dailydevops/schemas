namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of an inline function expression (XPath 3.0, 3.1.7): one function item that holds the closure.</summary>
internal sealed class FunctionItemNode : ExecutableNode
{
    private readonly FunctionSignature _signature;
    private readonly XPathQName[] _parameters;
    private readonly ExecutableNode _body;
    private readonly XPathQName[] _free;
    private readonly int[] _freeSlots;

    /// <summary>Initializes a new instance of the <see cref="FunctionItemNode"/> class.</summary>
    /// <param name="signature">The signature.</param>
    /// <param name="parameters">The expanded names of the parameters.</param>
    /// <param name="body">The body.</param>
    /// <param name="free">The free variables of the body, which the function captures by value.</param>
    /// <param name="freeSlots">The host slot each free variable is read from, -1 for a variable that is bound by name; parallel to <paramref name="free"/>.</param>
    public FunctionItemNode(
        FunctionSignature signature,
        XPathQName[] parameters,
        ExecutableNode body,
        XPathQName[] free,
        int[] freeSlots
    )
    {
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(free);
        ArgumentNullException.ThrowIfNull(freeSlots);

        _signature = signature;
        _parameters = parameters;
        _body = body;
        _free = free;
        _freeSlots = freeSlots;
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // A free variable that has no value yet is not captured: reading it in the body is XPDY0002 as everywhere else.
        var names = new XPathQName[_free.Length];
        var values = new XdmSequence[_free.Length];
        var count = 0;
        for (var i = 0; i < _free.Length; i++)
        {
            var variables = context.Variables;
            if (
                _freeSlots[i] >= 0
                    ? variables.TryGetSlot(_freeSlots[i], out var value)
                    : variables.TryGet(_free[i], out value)
            )
            {
                names[count] = _free[i];
                values[count++] = value;
            }
        }

        if (count < names.Length)
        {
            System.Array.Resize(ref names, count);
            System.Array.Resize(ref values, count);
        }

        return XdmSequence.Create(
            XdmItem.FromFunction(new InlineFunction(_signature, _parameters, _body, names, values))
        );
    }
}
