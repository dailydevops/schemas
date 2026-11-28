namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a variable reference (XPath 2.0, 3.1.2): the value bound for the evaluation, which a range variable of an enclosing expression may have shadowed.</summary>
internal sealed class VariableNode : ExecutableNode
{
    private readonly XPathQName _name;

    /// <summary>Initializes a new instance of the <see cref="VariableNode"/> class.</summary>
    /// <param name="name">The expanded name of the variable.</param>
    /// <exception cref="System.ArgumentException">The local name of <paramref name="name"/> is empty.</exception>
    public VariableNode(XPathQName name)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name.LocalName, nameof(name));

        _name = name;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The variable is not bound (XPDY0002).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Variables.Get(_name);
    }
}
