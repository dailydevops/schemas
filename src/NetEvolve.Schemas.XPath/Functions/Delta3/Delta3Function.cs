namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The base of the functions and arities that XPath 3.0 and 3.1 add to the <c>fn</c> namespace (Functions and Operators 3.1): each one
/// is visible from XPath 3.0 on and unknown in XPath 2.0 mode, so the registry reports XPST0017 for it there.
/// </summary>
/// <remarks>
/// Limitation: an arity that XPath 2.0 already has (the two-argument <c>fn:string-join</c>) stays with its 2.0 function; the new arity
/// is a function of its own over the same name, which the registry accepts because the version masks do not overlap.
/// </remarks>
internal abstract class Delta3Function : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="Delta3Function"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected Delta3Function(string localName, int minArity, int maxArity, IReadOnlyList<XPathParameterType> parameters)
        : base(localName, minArity, maxArity, parameters) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <summary>Gets <c>node()?</c>.</summary>
    protected static XPathParameterType OptionalNode => new(XPathItemType.Node, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>node()*</c>.</summary>
    protected static XPathParameterType NodeItems => new(XPathItemType.Node, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>item()*</c>.</summary>
    protected static XPathParameterType Items => new(XPathItemType.Item, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>xs:string?</c>.</summary>
    protected static XPathParameterType OptionalString => new(XPathItemType.String, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:string</c> exactly once.</summary>
    protected static XPathParameterType OneString => new(XPathItemType.String, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:string*</c>.</summary>
    protected static XPathParameterType StringItems => new(XPathItemType.String, Occurrence.ZeroOrMore);
}
