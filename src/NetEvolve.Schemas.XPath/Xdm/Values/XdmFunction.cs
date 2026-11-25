namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Types;

/// <summary>
/// A function item of the data model (XPath 3.0; Data Model 3.0, function items): an optional name, the arity, the signature, and
/// the function body together with the closure that the subclass holds. Immutable; a function item is equal to itself only.
/// </summary>
/// <remarks>
/// Limitation: the annotations of a function item and the base URI are not part of the model (they serve XQuery and the later maps and arrays).
/// </remarks>
internal abstract class XdmFunction
{
    private readonly XPathQName? _name;

    /// <summary>Initializes a new instance of the <see cref="XdmFunction"/> class.</summary>
    /// <param name="name">The name; <see langword="null"/> for an anonymous function.</param>
    /// <param name="signature">The signature.</param>
    /// <exception cref="ArgumentNullException"><paramref name="signature"/> is <see langword="null"/>.</exception>
    protected XdmFunction(XPathQName? name, FunctionSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);

        _name = name;
        Signature = signature;
    }

    /// <summary>Gets the name, <see langword="null"/> for an anonymous function.</summary>
    public XPathQName? Name => _name;

    /// <summary>Gets the number of parameters.</summary>
    public int Arity => Signature.Arity;

    /// <summary>Gets the signature.</summary>
    public FunctionSignature Signature { get; }

    /// <summary>Calls the function with arguments that match its arity; the function converts them to its parameter types.</summary>
    /// <param name="context">The evaluation context of the call.</param>
    /// <param name="arguments">The arguments, one per parameter.</param>
    /// <returns>The result.</returns>
    /// <exception cref="NetEvolve.Schemas.XPath.Errors.XPathException">An argument or the result does not match the declared type (XPTY0004), or the body raises an error.</exception>
    public abstract XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments);
}
