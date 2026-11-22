namespace NetEvolve.Schemas.XPath.Functions.Documents;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:unparsed-text-lines($href as xs:string?[, $encoding as xs:string]) as xs:string*</c> (Functions and Operators 3.1, 14.9.3): the
/// lines of the text of the resource, in order. A line ends at a line feed, a carriage return, or a carriage return and a line feed; the
/// line ending is no part of the line, and a line ending at the end of the text starts no further line, so the empty text gives the empty
/// sequence. The empty sequence gives the empty sequence. It is available in XPath 3.0 and 3.1 and not under the XSLT 2.0 host flag.
/// </summary>
/// <remarks>
/// Errors and limitations are those of <see cref="UnparsedTextFunction"/>. The next line (U+0085) and the line separator (U+2028)
/// are no line ending, as the specification states.
/// </remarks>
internal sealed class UnparsedTextLinesFunction : TextResourceFunction
{
    /// <summary>Initializes a new instance of the <see cref="UnparsedTextLinesFunction"/> class.</summary>
    /// <param name="staticBaseUri">The static base URI, <see langword="null"/> for none.</param>
    public UnparsedTextLinesFunction(Uri? staticBaseUri = null)
        : base("unparsed-text-lines", staticBaseUri) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    /// <exception cref="XPathException">The resource cannot be read as text (FOUT1170, FOUT1190, FOUT1200).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        if (invocation.Arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        if (!TryLoadText(ref invocation, out var text, out var error))
        {
            throw DocumentErrors.Exception(error);
        }

        // The list is bounded by the sequence limit of the context and returns its pooled array when disposed.
        var lines = new XdmItemList(invocation.Context.Limits.MaxSequenceSize, new SourceSpan(-1, 0));
        try
        {
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is not ('\n' or '\r'))
                {
                    continue;
                }

                lines.Add(XdmItem.FromString(text.Substring(start, i - start)));
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }

            if (start < text.Length)
            {
                lines.Add(XdmItem.FromString(text.Substring(start)));
            }

            invocation.Result = lines.ToSequence();
        }
        finally
        {
            lines.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }
}
