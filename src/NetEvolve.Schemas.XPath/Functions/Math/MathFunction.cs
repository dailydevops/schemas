namespace NetEvolve.Schemas.XPath.Functions.Math;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A function of the <c>math</c> namespace (Functions and Operators 3.1, 4.8): available from XPath 3.0 on, with <c>xs:double</c>
/// parameters (the function conversion rules promote the other numeric types) and one <c>xs:double</c> result. A function whose
/// first argument may be empty gives the empty sequence for it.
/// </summary>
/// <remarks>
/// Limitation: the results come from <see cref="System.Math"/>, which follows IEEE 754-2008 for the special values the specification
/// lists; the last bit of a transcendental result is that of the platform math library, so it is identical on every target
/// framework of one platform, and a platform that rounds differently shows its own last bit. <c>math:pow</c> converts its second
/// argument (<c>xs:numeric</c> in the specification) to <c>xs:double</c> by the function conversion rules, which is the value the
/// specification computes with.
/// </remarks>
internal sealed class MathFunction : XPathFunction
{
    private const string MathNamespace = "http://www.w3.org/2005/xpath-functions/math";

    private readonly Func<double, double, double> _compute;

    /// <summary>Initializes a new instance of the <see cref="MathFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="arity">The number of arguments: 0 to 2.</param>
    /// <param name="optionalFirst"><see langword="true"/> when the first argument is <c>xs:double?</c>, <see langword="false"/> when it is <c>xs:double</c>.</param>
    /// <param name="compute">The result from the arguments; the second is 0 when there is none.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="compute"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    public MathFunction(string localName, int arity, bool optionalFirst, Func<double, double, double> compute)
        : base(new XPathQName(MathNamespace, localName), arity, arity, Parameters(arity, optionalFirst))
    {
        ArgumentNullException.ThrowIfNull(compute);

        _compute = compute;
    }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments.Length > 0 && arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        // The function conversion has made every argument one xs:double.
        var first = 0d;
        var second = 0d;
        if (arguments.Length > 0)
        {
            _ = arguments[0][0].TryGetDouble(out first);
        }

        if (arguments.Length > 1)
        {
            _ = arguments[1][0].TryGetDouble(out second);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromDouble(_compute(first, second)));
        return XPathEvaluationStatus.Ok;
    }

    private static XPathParameterType[] Parameters(int arity, bool optionalFirst)
    {
        var parameters = new XPathParameterType[arity];
        for (var i = 0; i < arity; i++)
        {
            parameters[i] = new XPathParameterType(
                XPathItemType.Double,
                i == 0 && optionalFirst ? Occurrence.ZeroOrOne : Occurrence.ExactlyOne
            );
        }

        return parameters;
    }
}
