namespace NetEvolve.Schemas.Xml.Dtd.Validation.Document;

using System.Collections.Generic;

/// <summary>
/// Implemented by a declaration model that records the parse-time facts of design #122, D4; <see cref="DtdDocumentStep"/>
/// turns them into issues at <c>BeginDocument</c>.
/// </summary>
/// <remarks>
/// Limitation: the parser does not record them yet (#608), so a parsed document reports none of the four constraints that
/// depend on them (Unique Element Type Declaration, Proper Declaration/PE Nesting, Proper Group/PE Nesting, Proper
/// Conditional Section/PE Nesting); hand-built models in the tests do.
/// </remarks>
internal interface IDtdAnomalySource
{
    /// <summary>Gets the recorded facts in the order in which they were found.</summary>
    IReadOnlyList<DtdAnomaly> Anomalies { get; }
}
