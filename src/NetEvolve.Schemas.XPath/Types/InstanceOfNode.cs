namespace NetEvolve.Schemas.XPath.Types;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of <c>E instance of T</c> (XPath 2.0, 3.10.1): a single boolean.</summary>
/// <remarks>Limitation: nodes are untyped until schema-aware processing fills the annotation slot (XSD-16).</remarks>
internal sealed class InstanceOfNode : ExecutableNode
{
    private readonly ExecutableNode _operand;
    private readonly SequenceTypeMatcher _type;
    private readonly IXdmTypeAnnotationSource _annotations;

    /// <summary>Initializes a new instance of the <see cref="InstanceOfNode"/> class.</summary>
    /// <param name="operand">The operand.</param>
    /// <param name="type">The sequence type.</param>
    /// <param name="annotations">The slot that supplies the type annotation of a node.</param>
    public InstanceOfNode(ExecutableNode operand, SequenceTypeMatcher type, IXdmTypeAnnotationSource annotations)
    {
        ArgumentNullException.ThrowIfNull(operand);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(annotations);

        _operand = operand;
        _type = type;
        _annotations = annotations;
    }

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = _operand.Evaluate(context);
        return XdmSequence.Create(XdmItem.FromBoolean(_type.Matches(value, _annotations)));
    }
}
