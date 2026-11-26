namespace NetEvolve.Schemas.XPath.Functions.Numerics;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The base of the numeric and boolean functions: every one is a built-in function of the <c>fn</c> namespace, and the parameter types they share are declared once.</summary>
/// <remarks>
/// Limitations: the group follows XPath 2.0 and its Functions and Operators 1.0, sections 6.4, 9, and 2 (<c>fn:number</c>); the XPath 1.0
/// compatibility mode converts the arguments of a call before the function sees them (<see cref="Compatibility.CompatibilityFunctionConversion"/>). <c>fn:round</c> with a precision is a function of Functions and Operators 3.0 and is registered
/// for XPath 3.0 and 3.1 only (<see cref="RoundWithPrecisionFunction"/>).
/// </remarks>
internal abstract class NumericFunction : XPathFunction
{
    private static readonly string FunctionNamespace = XPathStaticContext.Default.DefaultFunctionNamespace;

    /// <summary>Initializes a new instance of the <see cref="NumericFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected NumericFunction(
        string localName,
        int minArity,
        int maxArity,
        IReadOnlyList<XPathParameterType> parameters
    )
        : base(new XPathQName(FunctionNamespace, localName), minArity, maxArity, parameters) { }

    /// <summary>Gets <c>numeric?</c>: the argument is atomized, and an <c>xs:untypedAtomic</c> value is cast to <c>xs:double</c>.</summary>
    protected static XPathParameterType OptionalNumeric => new(XPathItemType.Numeric, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:integer</c> exactly once.</summary>
    protected static XPathParameterType OneInteger => new(XPathItemType.Integer, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:anyAtomicType?</c>: the argument is atomized, and nothing is cast.</summary>
    protected static XPathParameterType OptionalAtomic => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>item()*</c>: the argument is neither atomized nor read by the function conversion rules.</summary>
    protected static XPathParameterType Items => new(XPathItemType.Item, Occurrence.ZeroOrMore);
}
