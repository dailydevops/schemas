namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of <c>E cast as T</c> and <c>E cast as T?</c> (XPath 2.0, 3.10.2).</summary>
/// <remarks>
/// The operand is atomized. The empty sequence gives the empty sequence with the optional marker and XPTY0004 without it; more than
/// one item is XPTY0004. A single value is cast through the casting table, so a failed cast raises FORG0001, FOCA0001, FOCA0002, FOCA0003,
/// or XPTY0004 for a pair that is not allowed. Limitation: nodes are untyped until schema-aware processing fills the annotation slot (XSD-16).
/// </remarks>
internal sealed class CastNode : ExecutableNode
{
    private readonly ExecutableNode _operand;
    private readonly XdmTypeId _target;
    private readonly bool _allowsEmpty;
    private readonly CastingTable _table;
    private readonly INamespaceContext? _namespaces;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="CastNode"/> class.</summary>
    /// <param name="operand">The operand.</param>
    /// <param name="target">The target type.</param>
    /// <param name="allowsEmpty">Whether the optional marker is present.</param>
    /// <param name="table">The casting table.</param>
    /// <param name="location">The location of the expression, reported with a dynamic error.</param>
    /// <param name="namespaces">The namespaces that resolve the prefix of a lexical form, <see langword="null"/> when the operand may not be cast from its lexical form.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="operand"/> or <paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="target"/> is <see langword="default"/>.</exception>
    public CastNode(
        ExecutableNode operand,
        XdmTypeId target,
        bool allowsEmpty,
        CastingTable table,
        SourceSpan location,
        INamespaceContext? namespaces = null
    )
    {
        Argument.ThrowIfNull(operand);
        Argument.ThrowIfLessThan(target.Value, 1);
        Argument.ThrowIfNull(table);

        _operand = operand;
        _target = target;
        _allowsEmpty = allowsEmpty;
        _table = table;
        _location = location;
        _namespaces = namespaces;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">The cast fails (FORG0001, FOCA0001, FOCA0002, FOCA0003, XPTY0004).</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        Argument.ThrowIfNull(context);

        var value = XPathAtomization.Atomize(_operand.Evaluate(context));
        if (value.Count == 0 && _allowsEmpty)
        {
            return XdmSequence.Empty;
        }

        if (value.Count != 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, _location.Offset, _location.Length);
        }

        var status = _table.TryCast(value[0], _target, _namespaces, out var result);
        return status == CastStatus.Ok
            ? XdmSequence.Create(result)
            : throw XPathErrors.Exception(status.ToErrorCode(), _location.Offset, _location.Length);
    }
}
