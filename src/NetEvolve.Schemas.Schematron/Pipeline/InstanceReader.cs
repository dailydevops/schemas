namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// Reads an instance into the tree that the rules run on, through the loader of the Xml package for every input kind: bytes, text, and
/// both as one segment or as a sequence. There is no parse path of the pipeline's own (SCH-55, #311); a stream has been read into a
/// buffer by the base of the validator before it gets here, so it is a thin adapter.
/// </summary>
/// <remarks>
/// The limits of the validator reach the reader through <see cref="XmlIssues.Options"/>. A failure of the reader is an error issue
/// with the code and position of the reader; no rule runs on an instance that is not a tree.
/// </remarks>
internal static class InstanceReader
{
    /// <summary>Reads the instance.</summary>
    /// <param name="source">The instance.</param>
    /// <param name="limits">The limits of the validator.</param>
    /// <param name="tree">The tree of the instance when the read succeeded.</param>
    /// <param name="issue">The issue of the failure when the read failed.</param>
    /// <returns><see langword="true"/> when the instance is a tree.</returns>
    internal static bool TryRead(
        in DocumentSource source,
        ValidationLimits limits,
        out XmlDocumentTree tree,
        out ValidationIssue issue
    )
    {
        Argument.ThrowIfNull(limits);

        var options = XmlIssues.Options(limits);
        var baseUri = Uri.TryCreate(source.SystemId, UriKind.Absolute, out var absolute) ? absolute : null;
        try
        {
            tree = Load(in source, options, baseUri);
            issue = null!;
            return true;
        }
        catch (XmlParseException failure)
        {
            tree = null!;
            issue = XmlIssues.Issue(failure, source.SystemId);
            return false;
        }
    }

    private static XmlDocumentTree Load(in DocumentSource source, XmlReaderOptions options, Uri? baseUri)
    {
        if (source.Kind == DocumentSourceKind.Bytes)
        {
            return source.IsSingleSegment
                ? XmlDocumentLoader.Load(source.Bytes, options, baseUri)
                : XmlDocumentLoader.Load(source.ByteSequence, options, baseUri);
        }

        return source.IsSingleSegment
            ? XmlDocumentLoader.Load(source.Chars, options, baseUri)
            : XmlDocumentLoader.Load(source.CharSequence, options, baseUri);
    }
}
