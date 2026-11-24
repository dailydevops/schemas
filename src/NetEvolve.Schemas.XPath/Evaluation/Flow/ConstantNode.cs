namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of a literal (XPath 2.0, 3.1.1) and of the empty sequence <c>()</c>: a value that was built while the expression was lowered.</summary>
internal sealed class ConstantNode : ExecutableNode
{
    private readonly XdmSequence _value;

    /// <summary>Initializes a new instance of the <see cref="ConstantNode"/> class.</summary>
    /// <param name="value">The value.</param>
    public ConstantNode(XdmSequence value) => _value = value;

    /// <summary>Gets the value.</summary>
    public XdmSequence Value => _value;

    /// <inheritdoc />
    /// <exception cref="System.ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _value;
    }
}
