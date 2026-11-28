namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The executable form of <c>E castable as T</c> and <c>E castable as T?</c> (XPath 2.0, 3.10.3): a single boolean, and no dynamic error of the cast.</summary>
/// <remarks>
/// It runs the same casting table as <see cref="CastNode"/> and reads the status, so no exception is thrown for a failed cast. An error of
/// the operand itself still propagates. Limitation: the conversion is carried out to find out whether it succeeds.
/// </remarks>
internal sealed class CastableNode : ExecutableNode
{
    private readonly ExecutableNode _operand;
    private readonly XdmTypeId _target;
    private readonly bool _allowsEmpty;
    private readonly CastingTable _table;
    private readonly INamespaceContext? _namespaces;

    /// <summary>Initializes a new instance of the <see cref="CastableNode"/> class.</summary>
    /// <param name="operand">The operand.</param>
    /// <param name="target">The target type.</param>
    /// <param name="allowsEmpty">Whether the optional marker is present.</param>
    /// <param name="table">The casting table.</param>
    /// <param name="namespaces">The namespaces that resolve the prefix of a lexical form, <see langword="null"/> when the operand may not be cast from its lexical form.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="operand"/> or <paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="target"/> is <see langword="default"/>.</exception>
    public CastableNode(
        ExecutableNode operand,
        XdmTypeId target,
        bool allowsEmpty,
        CastingTable table,
        INamespaceContext? namespaces = null
    )
    {
        ArgumentNullException.ThrowIfNull(operand);
        ArgumentOutOfRangeException.ThrowIfLessThan(target.Value, 1);
        ArgumentNullException.ThrowIfNull(table);

        _operand = operand;
        _target = target;
        _allowsEmpty = allowsEmpty;
        _table = table;
        _namespaces = namespaces;
    }

    /// <summary>Gets the operand.</summary>
    public ExecutableNode Operand => _operand;

    /// <inheritdoc />
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var value = XPathAtomization.Atomize(_operand.Evaluate(context), context.Annotations);
        var castable = value.Count switch
        {
            0 => _allowsEmpty,
            1 => _table.TryCast(value[0], _target, _namespaces, out _) == CastStatus.Ok,
            _ => false,
        };

        return XdmSequence.Create(XdmItem.FromBoolean(castable));
    }
}
