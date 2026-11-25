namespace NetEvolve.Schemas.XPath.Functions.Maps;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Maps;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of the functions of the <c>map</c> namespace (Functions and Operators 3.1, 17.1). They exist from XPath 3.1 on. A map
/// parameter is declared as <c>item()</c> and checked by the function, so that a value that is no map raises XPTY0004 as the function
/// conversion rules require.
/// </summary>
internal abstract class MapFunction : XPathFunction
{
    internal const string MapNamespace = "http://www.w3.org/2005/xpath-functions/map";

    /// <summary>Initializes a new instance of the <see cref="MapFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="parameters">The declared parameter types.</param>
    protected MapFunction(string localName, params XPathParameterType[] parameters)
        : base(new XPathQName(MapNamespace, localName), parameters.Length, parameters.Length, parameters) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V31;

    /// <summary>Gets <c>item()</c> exactly once: the argument is read as a map by the function itself.</summary>
    protected static XPathParameterType OneItem => new(XPathItemType.Item, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:anyAtomicType</c> exactly once: a key.</summary>
    protected static XPathParameterType OneKey => new(XPathItemType.AnyAtomic, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:anyAtomicType*</c>: a list of keys.</summary>
    protected static XPathParameterType Keys => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>item()*</c>: a value.</summary>
    protected static XPathParameterType AnyItems => new(XPathItemType.Item, Occurrence.ZeroOrMore);

    /// <summary>Reads a map argument.</summary>
    /// <param name="argument">The argument.</param>
    /// <returns>The map.</returns>
    /// <exception cref="XPathException">The argument is not exactly one map (XPTY0004).</exception>
    protected static XdmMap RequireMap(in XdmSequence argument) =>
        XdmMap.TryRead(in argument, out var map) ? map! : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
}
