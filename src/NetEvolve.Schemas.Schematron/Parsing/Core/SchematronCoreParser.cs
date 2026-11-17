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
/// the node model records positions for element starts only. <c>include</c> and rule-level <c>extends</c> are parsed into internal intermediates for stage Resolve (see <c>CoreParseRun.Inclusion.cs</c> for the places left
/// <c>SCH-UNSUPPORTED</c>); a <c>library</c> as main root is <c>SCH-UNSUPPORTED</c>; <c>visit-each</c>, <see langword="from"/>, <see langword="when"/>, <c>documents</c>, and <see langword="as"/> are read
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

    /// <summary>Parses an included document as the root element the place of its <c>include</c> or <c>extends</c> allows.</summary>
    /// <param name="node">The document node, or the root element.</param>
    /// <param name="systemId">The system identifier reported as the source of every location, or <see langword="null"/>.</param>
    /// <param name="edition">The edition of the including schema.</param>
    /// <param name="xsltBinding"><see langword="true"/> when the including schema has an XSLT query binding.</param>
    /// <param name="target">The place the document replaces.</param>
    /// <param name="problems">Receives the problems, in document order; none for <see cref="FragmentStatus.Unsupported"/> and <see cref="FragmentStatus.WrongRoot"/>.</param>
    /// <param name="fragment">The model, <see langword="null"/> unless the status is <see cref="FragmentStatus.Parsed"/>.</param>
    /// <param name="rootName">The local name of the root element.</param>
    /// <returns>The outcome.</returns>
    internal static FragmentStatus TryParseFragment(
        XmlNode node,
        string? systemId,
        SchematronEdition edition,
        bool xsltBinding,
        FragmentTarget target,
        IProblemSink problems,
        out SchematronNode? fragment,
        out string rootName
    )
    {
        Argument.ThrowIfNull(node.Tree);
        Argument.ThrowIfNull(problems);

        return new CoreParseRun(node.Tree, systemId, problems).RunFragment(
            node,
            edition,
            xsltBinding,
            target,
            out fragment,
            out rootName
        );
    }
}
