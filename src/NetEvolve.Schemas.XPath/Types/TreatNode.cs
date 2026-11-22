namespace NetEvolve.Schemas.XPath.Types;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of <c>E treat as T</c> (XPath 2.0, 3.10.5): the operand unchanged, or XPDY0050.</summary>
/// <remarks>Limitation: nodes are untyped until schema-aware processing fills the annotation slot (XSD-16).</remarks>
internal sealed class TreatNode : ExecutableNode
{
    private readonly ExecutableNode _operand;
    private readonly SequenceTypeMatcher _type;
    private readonly IXdmTypeAnnotationSource _annotations;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="TreatNode"/> class.</summary>
    /// <param name="operand">The operand.</param>
    /// <param name="type">The sequence type.</param>
    /// <param name="annotations">The slot that supplies the type annotation of a node.</param>
    /// <param name="location">The location of the expression, reported with XPDY0050.</param>
    public TreatNode(
        ExecutableNode operand,
        SequenceTypeMatcher type,
        IXdmTypeAnnotationSource annotations,
        SourceSpan location
    )
    {
        ArgumentNullException.ThrowIfNull(operand);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(annotations);

        _operand = operand;
        _type = type;
        _annotations = annotations;
        _location = location;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The operand does not match the sequence type (XPDY0050).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = _operand.Evaluate(context);
        return _type.Matches(value, _annotations)
            ? value
            : throw XPathErrors.Exception(XPathErrorCodes.XPDY0050, _location.Offset, _location.Length);
    }
}
