namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// One compiled <c>xsl:sort</c>: the key expression, the direction, and whether the values are compared as strings
/// (<c>data-type="text"</c>). It does nothing when executed, because the instruction that holds the sort key specification
/// (<c>xsl:for-each</c>, and later <c>xsl:for-each-group</c> and <c>xsl:apply-templates</c>) applies it with <see cref="XsltSorter"/>.
/// </summary>
internal sealed class XsltSortKey : XsltExecutable, IXsltSlotReader
{
    internal XsltSortKey(QueryExpression select, bool descending, bool asText, DocumentLocation location)
    {
        Argument.ThrowIfNull(select);

        Select = select;
        Descending = descending;
        AsText = asText;
        Location = location;
    }

    /// <summary>Gets the key expression, evaluated with the focus of the initial sequence.</summary>
    internal QueryExpression Select { get; }

    /// <summary>Gets a value indicating whether the order is descending.</summary>
    internal bool Descending { get; }

    /// <summary>Gets a value indicating whether the key values are converted to strings before they are compared.</summary>
    internal bool AsText { get; }

    /// <summary>Gets the location of the <c>xsl:sort</c> element, for a problem.</summary>
    internal DocumentLocation Location { get; }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => Select.ReferencedSlots;

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        return QueryStatus.Ok;
    }
}
