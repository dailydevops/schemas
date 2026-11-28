namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The constructor function of an atomic type, <c>xs:T($arg as xs:anyAtomicType?) as xs:T?</c> (XPath 2.0, 3.10.4): the argument is
/// atomized by the function conversion rules, the empty sequence gives the empty sequence, and a value is cast to the type.
/// </summary>
/// <remarks>Stateless apart from the immutable table, so it is safe to share.</remarks>
internal sealed class ConstructorFunction : XPathFunction
{
    private static readonly XPathParameterType[] Parameters = [new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne)];

    private static readonly XdmTypeId DateTimeStampType = XdmTypes.Get("dateTimeStamp");

    private readonly XdmTypeId _target;
    private readonly CastingTable _table;

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

        var status = _table.TryCast(argument[0], _target, out var result);
        if (status == CastStatus.Ok)
        {
            invocation.Result = XdmSequence.Create(result);
            return XPathEvaluationStatus.Ok;
        }

        invocation.Diagnostic = XPathDiagnostic.Create(status.ToErrorCode());
        return XPathEvaluationStatus.Error;
    }
}
