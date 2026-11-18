namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The base of the sequence and aggregate functions: every one is a built-in function of the <c>fn</c> namespace, and the parameter types they share are declared once.</summary>
/// <remarks>
/// Limitations: the group follows XPath 2.0 and its Functions and Operators 1.0, 15 Functions and Operators on Sequences, and the
/// codepoint collation is the only collation (#123 AP19, DEF-43). A function reads its argument whole unless its summary says
/// otherwise; <c>fn:count</c>, <c>fn:exists</c>, and <c>fn:empty</c> are the lazy ones, because the library produces no other lazy
/// sequence yet.
/// </remarks>
internal abstract class SequenceFunction : XPathFunction
{
    private static readonly string FunctionNamespace = XPathStaticContext.Default.DefaultFunctionNamespace;

    /// <summary>Initializes a new instance of the <see cref="SequenceFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected SequenceFunction(
        string localName,
        int minArity,
        int maxArity,
        IReadOnlyList<XPathParameterType> parameters
    )
        : base(new XPathQName(FunctionNamespace, localName), minArity, maxArity, parameters) { }

    /// <summary>Gets <c>item()*</c>: the argument is neither atomized nor read by the function conversion rules.</summary>
    protected static XPathParameterType Items => new(XPathItemType.Item, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>xs:anyAtomicType*</c>: the argument is atomized.</summary>
    protected static XPathParameterType Atomics => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>xs:anyAtomicType</c> exactly once.</summary>
    protected static XPathParameterType OneAtomic => new(XPathItemType.AnyAtomic, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:anyAtomicType?</c>.</summary>
    protected static XPathParameterType OptionalAtomic => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:string</c> exactly once: the collation argument.</summary>
    protected static XPathParameterType Collation => new(XPathItemType.String, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:double</c> exactly once; an <c>xs:integer</c>, <c>xs:decimal</c>, or <c>xs:float</c> argument is promoted.</summary>
    protected static XPathParameterType OneDouble => new(XPathItemType.Double, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:integer</c> exactly once.</summary>
    protected static XPathParameterType OneInteger => new(XPathItemType.Integer, Occurrence.ExactlyOne);
}
