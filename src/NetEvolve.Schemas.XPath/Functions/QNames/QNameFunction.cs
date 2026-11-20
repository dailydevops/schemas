namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Nodes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of the QName and URI functions: every one is a built-in function of the <c>fn</c> namespace, and the parameter types they
/// share are declared once, together with the way to read a string argument and an element argument.
/// </summary>
/// <remarks>
/// Limitations: the group follows XPath 2.0 and its Functions and Operators 1.0, 11 Functions Related to QNames, 8 Functions on
/// anyURI, and <c>fn:base-uri</c> of 14. The function conversion has no <c>xs:QName</c> and no <c>element()</c> item type (the kind
/// tests belong to the sequence type matcher, #227), so a QName parameter is declared <c>xs:anyAtomicType?</c> and an element parameter
/// <c>node()</c>; the function itself raises XPTY0004 for an atomic value that is no QName and for a node that is no element.
/// </remarks>
internal abstract class QNameFunction : NodeFunction
{
    /// <summary>Initializes a new instance of the <see cref="QNameFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="maxArity">The maximum number of arguments.</param>
    /// <param name="parameters">The declared parameter types.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> or <paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The arity range is invalid, or the number of parameter types lies outside it.</exception>
    protected QNameFunction(string localName, int minArity, int maxArity, IReadOnlyList<XPathParameterType> parameters)
        : base(localName, minArity, maxArity, parameters) { }

    /// <summary>Gets <c>xs:string?</c>: the argument is atomized, an <c>xs:untypedAtomic</c> is cast, and an <c>xs:anyURI</c> is promoted.</summary>
    protected static XPathParameterType OptionalString => new(XPathItemType.String, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>xs:string</c> exactly once.</summary>
    protected static XPathParameterType OneString => new(XPathItemType.String, Occurrence.ExactlyOne);

    /// <summary>Gets <c>xs:QName?</c>, declared as <c>xs:anyAtomicType?</c> (see the remarks of the class).</summary>
    protected static XPathParameterType OptionalQName => new(XPathItemType.AnyAtomic, Occurrence.ZeroOrOne);

    /// <summary>Gets <c>element()</c> exactly once, declared as <c>node()</c> (see the remarks of the class).</summary>
    protected static XPathParameterType OneElement => new(XPathItemType.Node, Occurrence.ExactlyOne);

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

    /// <summary>Reads an argument that the function conversion has matched against <c>node()</c> exactly once.</summary>
    /// <param name="argument">The argument: one node.</param>
    /// <returns>The element.</returns>
    /// <exception cref="XPathException">The node is no element (XPTY0004).</exception>
    protected static XPathNode Element(in XdmSequence argument)
    {
        _ = argument[0].TryGetNode(out var node);
        return node.Kind == XdmNodeKind.Element ? node : throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
    }
}
