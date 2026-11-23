namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of the string functions: every one is a built-in function of the <c>fn</c> namespace, and the parameter types they share are
/// declared once, together with the way to read a string argument and to return a string.
/// </summary>
/// <remarks>
/// Limitations: the group follows XPath 2.0 and its Functions and Operators 1.0, 7 Functions on Strings. All functions count and index in
/// Unicode code points. The collation arguments accept the codepoint collation only (#123 AP19, DEF-43 (#55), DEF-76 (#88)), which
/// <c>CollationArgument</c> checks, and the case mapping is culture-invariant. A function reads its argument whole and builds its result
/// in a stack buffer that grows into a pooled array, so only the result string is allocated.
/// </remarks>
internal abstract class TextFunction : XPathFunction
{
    private static readonly string FunctionNamespace = XPathStaticContext.Default.DefaultFunctionNamespace;

    /// <summary>Initializes a new instance of the <see cref="TextFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected TextFunction(string localName, int minArity, int maxArity, IReadOnlyList<XPathParameterType> parameters)
        : base(new XPathQName(FunctionNamespace, localName), minArity, maxArity, parameters) { }

    /// <summary>Gets <c>xs:string?</c>: the argument is atomized, an <c>xs:untypedAtomic</c> is cast, and an <c>xs:anyURI</c> is promoted.</summary>
    protected static XPathParameterType OptionalString => new(XPathItemType.String, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:string</c> exactly once.</summary>
    protected static XPathParameterType OneString => new(XPathItemType.String, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:string*</c>.</summary>
    protected static XPathParameterType StringItems => new(XPathItemType.String, Occurrence.ZeroOrMore);

    /// <summary>Gets <c>xs:anyAtomicType?</c>.</summary>
    protected static XPathParameterType OptionalAtomic => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:double</c> exactly once; an <c>xs:integer</c>, <c>xs:decimal</c>, or <c>xs:float</c> argument is promoted.</summary>
    protected static XPathParameterType OneDouble => new(XPathItemType.Double, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:integer*</c>.</summary>
    protected static XPathParameterType Integers => new(XPathItemType.Integer, Occurrence.ZeroOrMore);

    /// <summary>Reads an argument that the function conversion has matched against <c>xs:string?</c>.</summary>
    /// <param name="argument">The argument: empty or one string.</param>
    /// <returns>The string; the zero-length string for the empty sequence.</returns>
    protected static string Text(in XdmSequence argument)
    {
        if (argument.IsEmpty)
        {
            return string.Empty;
        }

        _ = argument[0].TryGetString(out var text);
        return text!;
    }

    /// <summary>Reads the string of a call that has no argument: the string value of the context item.</summary>
    /// <param name="invocation">The call.</param>
    /// <returns>The string value of the context item.</returns>
    /// <exception cref="XPathException">The context item is absent (XPDY0002).</exception>
    protected static string ContextText(ref XPathFunctionCall invocation)
    {
        var item = invocation.Context.ContextItem ?? throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
        return item.TryGetNode(out var node) ? node.StringValue() : StringFunction.OfAtomic(in item);
    }

    /// <summary>Sets the result of a call to one string.</summary>
    /// <param name="invocation">The call.</param>
    /// <param name="text">The string.</param>
    protected static void Return(ref XPathFunctionCall invocation, string text) =>
        invocation.Result = XdmSequence.Create(XdmItem.FromString(text));
}
