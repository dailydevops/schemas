namespace NetEvolve.Schemas.XPath.Functions.Arrays;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// A function of the <c>array</c> namespace (Functions and Operators 3.1, 17.3): a name, a signature, and a body from
/// <see cref="ArrayBodies"/>. The functions exist from XPath 3.1 on; a call in an earlier mode is XPST0017.
/// </summary>
/// <remarks>
/// Limitations: a parameter of type <c>array(*)</c> or <c>function(...)</c> is declared as <c>item()</c> and checked by the body, so a
/// wrong argument is XPTY0004 as the specification states; a function argument is called with the types of its own parameters, which
/// is the function coercion of the specification for every argument that the specification admits.
/// </remarks>
internal sealed class ArrayFunction : XPathFunction
{
    internal const string ArrayNamespace = "http://www.w3.org/2005/xpath-functions/array";

    private readonly ArrayBody _body;

    /// <summary>Initializes a new instance of the <see cref="ArrayFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="minArity">The minimum number of arguments.</param>
    /// <param name="parameters">The declared parameter types; their number is the maximum number of arguments.</param>
    /// <param name="body">The body.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/>, <paramref name="parameters"/>, or <paramref name="body"/> is <see langword="null"/>.</exception>
    public ArrayFunction(string localName, int minArity, XPathParameterType[] parameters, ArrayBody body)
        : base(new XPathQName(ArrayNamespace, localName), minArity, parameters.Length, parameters)
    {
        ArgumentNullException.ThrowIfNull(body);

        _body = body;
    }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => _body(ref invocation);
}
