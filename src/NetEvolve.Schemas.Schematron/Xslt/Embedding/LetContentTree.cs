namespace NetEvolve.Schemas.Schematron.Xslt.Embedding;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Builds the document node of a <see langword="let"/> with element content from the content as the parser kept it. The nodes go through
/// the content writer of the construction folder, which writes the start tag of an element once its attributes are known and runs the
/// namespace fixup (XSLT 2.0 section 5.7.3), so no second tree builder or fixup exists.
/// </summary>
internal static class LetContentTree
{
    /// <summary>Builds the tree.</summary>
    /// <param name="names">The name table of the temporary trees of the run.</param>
    /// <param name="content">The content of the <see langword="let"/>.</param>
    /// <param name="document">The document node when the result is <see langword="true"/>.</param>
    /// <param name="problem">The problem, without a location, when the result is <see langword="false"/>.</param>
    /// <returns><see langword="false"/> when a name could not be interned or a node was not allowed.</returns>
    internal static bool TryBuild(
        XmlNameTable names,
        IReadOnlyList<SchematronNode> content,
        out XdmItem document,
        out SchematronProblem problem
    )
    {
        Argument.ThrowIfNull(names);
        Argument.ThrowIfNull(content);

        using var sink = new TreeContentSink(names);
        var pass = new Pass(sink);
        var writer = ContentWriter.ForDocument(sink, sink.Content);
        pass.Write(content, writer);
        pass.Note(writer);
        if (pass.Problem is { } found)
        {
            document = default;
            problem = found;
            return false;
        }

        document = sink.Document;
        problem = default;
        return true;
    }

    // The key of an attribute is its expanded name in Clark form: "{uri}local", or "local" without a namespace.
    private static void SplitName(string key, out string uri, out string local)
    {
        var end = key.Length > 0 && key[0] == '{' ? key.AsSpan().IndexOf('}') : -1;
        uri = end < 0 ? string.Empty : key.Substring(1, end - 1);
        local = end < 0 ? key : key.Substring(end + 1);
    }

    private static bool IsBlank(string text)
    {
        foreach (var c in text)
        {
            if (c is not (' ' or '\t' or '\n' or '\r'))
            {
                return false;
            }
        }

        return true;
    }

    // One build: the first problem of any writer ends it, and nothing after it is written.
    private sealed class Pass
    {
        private readonly ISequenceSink _owner;

        internal Pass(ISequenceSink owner) => _owner = owner;

        internal SchematronProblem? Problem { get; private set; }

        internal void Note(ContentWriter writer) => Problem ??= writer.Problem;

        internal void Write(IReadOnlyList<SchematronNode> children, ContentWriter writer)
        {
            for (var i = 0; i < children.Count && Problem is null; i++)
            {
                switch (children[i])
                {
                    case SchematronText text:
                        if (!IsBlank(text.Text))
                        {
                            writer.Text(text.Text.AsSpan());
                        }

                        break;
                    case SchematronForeignElement element:
                        WriteElement(element, writer);
                        break;
                }

                Note(writer);
            }
        }

        private void WriteElement(SchematronForeignElement element, ContentWriter parent)
        {
            // Reading the content of the parent writes its start tag, which its attributes and namespaces are complete for.
            var target = parent.Content;
            Note(parent);
            if (Problem is not null)
            {
                return;
            }

            var writer = ContentWriter.ForElement(
                _owner,
                target,
                parent.ChildScope,
                string.Empty,
                element.LocalName,
                element.NamespaceUri
            );
            foreach (var attribute in element.Attributes)
            {
                SplitName(attribute.Key, out var uri, out var local);
                _ = writer.TryAddAttribute(string.Empty, local, uri, attribute.Value);
            }

            Write(element.Children, writer);
            writer.Finish();
            Note(writer);
        }
    }
}
