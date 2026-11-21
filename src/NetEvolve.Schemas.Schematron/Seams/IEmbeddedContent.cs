namespace NetEvolve.Schemas.Schematron.Seams;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The places where Schematron embeds content that the XSLT construction builds (SCH-84, #289): the element content of a
/// <see langword="let"/>, which becomes a temporary tree bound to the slot of the variable, and the content of a <c>property</c> that holds
/// <c>xsl:copy-of</c>, which becomes a snapshot that the report writer puts into the finding. The stages that need it (the slot binder
/// and the message builder) know only this seam, so they stay free of the XSLT folders.
/// </summary>
internal interface IEmbeddedContent
{
    /// <summary>Builds the value of a <see langword="let"/> with element content and binds it to its slot.</summary>
    /// <param name="run">The run that holds the value.</param>
    /// <param name="slot">The slot of the variable.</param>
    /// <param name="content">The element content of the <see langword="let"/>, as the parser kept it.</param>
    /// <param name="problem">The problem when the result is <see cref="QueryStatus.Problem"/>.</param>
    /// <returns><see cref="QueryStatus.Ok"/> when the slot is bound.</returns>
    QueryStatus TryBindLetContent(
        IQueryRun run,
        int slot,
        IReadOnlyList<SchematronNode> content,
        out SchematronProblem problem
    );

    /// <summary>Begins the content of one referenced property.</summary>
    /// <returns>The builder, which receives the items and the text of the property in order.</returns>
    IFindingContentBuilder BeginFindingContent();
}
