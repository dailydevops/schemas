namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Parses the core Schematron elements (<c>schema</c>, <c>ns</c>, <c>pattern</c>, <c>rule</c>, <c>assert</c>,
/// <c>report</c>, <see langword="let"/>, <c>phase</c>, <c>active</c>, <c>diagnostics</c>, <c>diagnostic</c>, <c>title</c>, <c>p</c>) from the
/// Xml node model into the Schematron object model.
/// </summary>
/// <remarks>
/// <para>
/// Limitations: foreign attributes are skipped, and foreign elements are skipped except the direct children of <c>schema</c> and <c>library</c> (design #137 AP-3 as narrowed by #150 EP-11; see <c>CoreParseRun.Edition2025.cs</c>); the order of the children of one parent
/// is not checked, only whether a child may appear in the parent at all; the content of <c>emph</c>, <c>dir</c>, and
/// <c>span</c> admits the inline elements of its host; a text part carries the location of its parent element, because
/// the node model records positions for element starts only. The elements that later work packages parse (<c>include</c> and <c>extends</c>) report
/// <c>SCH-UNSUPPORTED</c> until then; a <c>library</c> as main root does too; <c>visit-each</c>, <see langword="from"/>, <see langword="when"/>, <c>documents</c>, and <see langword="as"/> are read
/// as raw text, and their rejection belongs to the issues that own them. Locations come from the tree when it is an
/// <see cref="NetEvolve.Schemas.Xml.Model.XmlDocumentTree"/> and are zero otherwise. Nesting of inline elements is bounded by the reader
/// depth limit; the rest of the recursion is bounded by the grammar.
/// </para>
/// </remarks>
internal static class SchematronCoreParser
{
    /// <summary>Parses a schema document.</summary>
    /// <param name="node">The document node, or the root element.</param>
    /// <param name="systemId">The system identifier reported as the source of every location, or <see langword="null"/>.</param>
    /// <param name="problems">Receives the problems, in document order.</param>
    /// <param name="schema">The model, <see langword="null"/> when a problem was reported.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryParse(XmlNode node, string? systemId, IProblemSink problems, out SchematronSchema? schema)
    {
        Argument.ThrowIfNull(node.Tree);
        Argument.ThrowIfNull(problems);

        schema = new CoreParseRun(node.Tree, systemId, problems).Run(node);
        return schema is not null;
    }

    /// <summary>Parses a library document: an include target of the 2025 edition.</summary>
    /// <param name="node">The document node, or the root element.</param>
    /// <param name="systemId">The system identifier reported as the source of every location, or <see langword="null"/>.</param>
    /// <param name="edition">The edition of the including schema; a library is an error under <see cref="SchematronEdition.Iso2016"/>.</param>
    /// <param name="xsltBinding"><see langword="true"/> when the including schema has an XSLT query binding.</param>
    /// <param name="problems">Receives the problems, in document order.</param>
    /// <param name="library">The model, <see langword="null"/> when a problem was reported.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryParseLibrary(
        XmlNode node,
        string? systemId,
        SchematronEdition edition,
        bool xsltBinding,
        IProblemSink problems,
        out SchematronLibrary? library
    )
    {
        Argument.ThrowIfNull(node.Tree);
        Argument.ThrowIfNull(problems);

        library = new CoreParseRun(node.Tree, systemId, problems).RunLibrary(node, edition, xsltBinding);
        return library is not null;
    }
}
