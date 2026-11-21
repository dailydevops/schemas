namespace NetEvolve.Schemas.Schematron.Xslt.Embedding;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The embedding of XSLT construction into Schematron elements (SCH-84, #289): the element content of a <see langword="let"/> is built
/// into a temporary tree and bound to the slot of the variable. One instance serves one run; every temporary tree of the run uses
/// its one name table, which is the run-owned, caller-supplied table of design #150 EP-8 and is never frozen by the library.
/// </summary>
/// <remarks>
/// <para>
/// The content of a <see langword="let"/> is data, not a stylesheet: elements of the XSLT namespace are copied as elements and never run,
/// so <c>xsl:message terminate="yes"</c> in a <see langword="let"/> terminates nothing. White space alone between nodes is
/// stripped, as in a stylesheet (XSLT 2.0 section 4.3).
/// </para>
/// <para>
/// Limitations: the parser keeps the namespace of an element and not its prefix or the namespace declarations of its source, so a
/// copied element declares its namespace as the default namespace and a namespaced attribute gets a generated prefix; names and
/// values are what a path addresses. The slot is held by the run through <see cref="IXsltFunctionRun"/>, the value hooks that
/// stylesheet functions use; a run without them cannot bind a tree.
/// </para>
/// </remarks>
internal sealed class EmbeddedContent : IEmbeddedContent
{
    private const int MaxDistinctNames = 4096;

    private readonly XmlNameTable _names;

    /// <summary>Initializes a new instance of the <see cref="EmbeddedContent"/> class over a new name table.</summary>
    internal EmbeddedContent()
        : this(new SpanInterningNameTable(MaxDistinctNames)) { }

    /// <summary>Initializes a new instance of the <see cref="EmbeddedContent"/> class.</summary>
    /// <param name="names">The name table of every temporary tree of the run; the library never freezes it.</param>
    internal EmbeddedContent(XmlNameTable names)
    {
        Argument.ThrowIfNull(names);

        _names = names;
    }

    /// <inheritdoc />
    public QueryStatus TryBindLetContent(
        IQueryRun run,
        int slot,
        IReadOnlyList<SchematronNode> content,
        out SchematronProblem problem
    )
    {
        Argument.ThrowIfNull(run);
        Argument.ThrowIfNull(content);

        if (run is not IXsltFunctionRun values)
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.XsltUnsupported,
                "The run cannot hold a temporary tree as the value of a variable.",
                default
            );
            return QueryStatus.Problem;
        }

        if (!LetContentTree.TryBuild(_names, content, out var document, out problem))
        {
            return QueryStatus.Problem;
        }

        values.SetValue(slot, XdmSequence.Create(document));
        return QueryStatus.Ok;
    }
}
