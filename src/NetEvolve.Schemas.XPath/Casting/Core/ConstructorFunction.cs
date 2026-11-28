namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The constructor function of an atomic type, <c>xs:T($arg as xs:anyAtomicType?) as xs:T?</c> (XPath 2.0, 3.10.4): the argument is
/// atomized by the function conversion rules, the empty sequence gives the empty sequence, and a value is cast to the type.
/// </summary>
/// <remarks>
/// Stateless apart from the immutable table, so it is safe to share. The lowering of a call binds a copy to the namespaces of the static
/// context for a target that resolves the prefix of a lexical form (<c>xs:QName</c>), see <see cref="BindTo"/>.
/// </remarks>
internal sealed class ConstructorFunction : XPathFunction
{
    private static readonly XPathParameterType[] Parameters = [new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne)];

    private static readonly XdmTypeId DateTimeStampType = XdmTypes.Get("dateTimeStamp");

    private readonly XdmTypeId _target;
    private readonly CastingTable _table;
    private readonly bool _bound;
    private readonly INamespaceContext? _namespaces;

    /// <summary>Initializes a new instance of the <see cref="ConstructorFunction"/> class.</summary>
    /// <param name="name">The expanded name of the function, the name of the type.</param>
    /// <param name="target">The type.</param>
    /// <param name="table">The casting table.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="table"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="target"/> is <see langword="default"/>.</exception>
    public ConstructorFunction(XPathQName name, XdmTypeId target, CastingTable table)
        : base(name, 1, 1, Parameters)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(target.Value, 1);
        ArgumentNullException.ThrowIfNull(table);

        _target = target;
        _table = table;
    }

    private ConstructorFunction(ConstructorFunction source, INamespaceContext? namespaces)
        : this(source.Name, source._target, source._table)
    {
        _bound = true;
        _namespaces = namespaces;
    }

    /// <summary>Binds the function to the static context of one call.</summary>
    /// <param name="argument">The syntax of the argument of the call.</param>
    /// <param name="statics">The static context of the call.</param>
    /// <returns>
    /// This function when its target does not depend on the namespaces; otherwise a copy that casts as <c>cast as xs:T?</c> does, with the namespaces
    /// of the static context and the rule of the language version (see <see cref="CastingLowerings.NamespacesFor"/>).
    /// </returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public ConstructorFunction BindTo(Expression argument, XPathStaticContext statics)
    {
        ArgumentNullException.ThrowIfNull(argument);
        ArgumentNullException.ThrowIfNull(statics);

        return _table.IsNamespaceSensitive(_target)
            ? new ConstructorFunction(this, CastingLowerings.NamespacesFor(argument, statics, _table, _target))
            : this;
    }

    /// <inheritdoc />
    /// <remarks><c>xs:dateTimeStamp</c> is a type of XPath 3.1 alone, so its constructor function is too.</remarks>
    public override XPathVersionMask Versions =>
        _target == DateTimeStampType ? XPathVersionMask.V31 : XPathVersionMask.All;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(invocation.Arguments.Length, 1);

        var argument = invocation.Arguments[0];
        if (argument.Count == 0)
        {
            invocation.Result = XdmSequence.Empty;
            return XPathEvaluationStatus.Ok;
        }

        var status = Cast(argument[0], out var result);
        if (status == CastStatus.Ok)
        {
            invocation.Result = XdmSequence.Create(result);
            return XPathEvaluationStatus.Ok;
        }

        invocation.Diagnostic = XPathDiagnostic.Create(status.ToErrorCode());
        return XPathEvaluationStatus.Error;
    }

    private CastStatus Cast(in XdmItem source, out XdmItem result) =>
        _bound
            ? _table.TryCast(in source, _target, _namespaces, out result)
            : _table.TryCast(in source, _target, out result);
}
