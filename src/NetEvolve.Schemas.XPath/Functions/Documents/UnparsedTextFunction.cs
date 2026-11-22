namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:unparsed-text($href as xs:string?[, $encoding as xs:string]) as xs:string?</c> (Functions and Operators 3.1, 14.9.1; XSLT 2.0,
/// 16.2): the text of the resource that the reference names, loaded through the resolver of the evaluation context, decoded, and
/// pooled. The empty sequence gives the empty sequence. It is available in XPath 3.0 and 3.1; <see cref="Xslt20UnparsedTextFunction"/>
/// adds XPath 2.0 under the XSLT 2.0 host flag.
/// </summary>
/// <remarks>
/// <para>
/// Errors: FOUT1170 when the resource cannot be retrieved: the reference is no URI reference or has a fragment identifier, it is relative
/// and the function holds no absolute static base URI, the context has no resolver, the resolver denies or does not know the resource, or
/// the resource exceeds the size limit. The default resolver denies every URI, so nothing is accessed. FOUT1190 for an encoding that is not
/// supported or that the bytes do not follow, FOUT1200 for a character that is no XML character (<see cref="DocumentErrors"/>).
/// </para>
/// <para>Limitations: see <see cref="TextDecoding"/> for the encodings, and <see cref="TextResourceFunction"/> for the base URI.</para>
/// </remarks>
internal class UnparsedTextFunction : TextResourceFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextFunction(Uri? staticBaseUri = null)
        : base("unparsed-text", staticBaseUri) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    /// <exception cref="XPathException">The resource cannot be read as text (FOUT1170, FOUT1190, FOUT1200).</exception>
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (invocation.Arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        if (!TryLoadText(ref invocation, out var text, out var error))
        {
            throw DocumentErrors.Exception(error);
        }

        invocation.Context.Limits.ThrowIfStringTooLong(text.Length, new SourceSpan(-1, 0));
        invocation.Result = XdmSequence.Create(XdmItem.FromString(text));
        return XPathEvaluationStatus.Ok;
    }
}
