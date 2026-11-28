namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of the functions on function items (Functions and Operators 3.0, 16.2 and 16.3). They are built-in functions of the <c>fn</c>
/// namespace that exist from XPath 3.0 on.
/// </summary>
internal abstract class FunctionItemFunction : XPathFunction
{
    internal const string FnNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Initializes a new instance of the <see cref="FunctionItemFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="parameters">The declared parameter types.</param>
    protected FunctionItemFunction(string localName, XPathParameterType[] parameters)
        : base(new XPathQName(FnNamespace, localName), parameters.Length, parameters.Length, parameters) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <summary>Gets <c>item()</c> exactly once: the argument is read as a function item by the function itself.</summary>
    protected static XPathParameterType OneItem => new(XPathItemType.Item, Occurrence.ExactlyOne);
}
