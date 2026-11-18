namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// What a validation step sees besides the events: the declaration view, the options, the facts of the document, and the
/// reporter. Issues go to the shared collector, so the drop policy at <c>MaxIssues</c> lives once.
/// </summary>
/// <remarks>
/// Limitation: the declaration view records neither the position of each declaration nor the anomalies that only the
/// declaration parser can see (the three nesting constraints and a dropped second element declaration, design #122, D4);
/// until it does, declaration-time checks report at <see cref="DocumentTypeLocation"/>. A location whose line is unknown
/// (zero, for example <see langword="default"/>) is replaced by <see cref="DocumentTypeLocation"/> in every report.
/// </remarks>
public sealed class DtdValidationContext
{
    private readonly ValidationIssueCollector _issues;

    /// <summary>Initializes a new instance of the <see cref="DtdValidationContext"/> class.</summary>
    /// <param name="declarations">The read-only declaration view.</param>
    /// <param name="options">The options.</param>
    /// <param name="issues">The collector that receives the issues.</param>
    /// <param name="isStandalone"><see langword="true"/> when the XML declaration says <c>standalone="yes"</c>.</param>
    /// <param name="systemId">The system identifier of the document, <see langword="null"/> when unknown.</param>
    /// <param name="documentTypeLocation">The location of the document type declaration, <see langword="default"/> when unknown.</param>
    public DtdValidationContext(
        IDtdDeclarations declarations,
        DtdValidationOptions options,
        ValidationIssueCollector issues,
        bool isStandalone,
        string? systemId,
        DocumentLocation documentTypeLocation
    )
    {
        Argument.ThrowIfNull(declarations);
        Argument.ThrowIfNull(options);
        Argument.ThrowIfNull(issues);

        Declarations = declarations;
        Options = options;
        _issues = issues;
        IsStandalone = isStandalone;
        SystemId = systemId;
        DocumentTypeLocation = documentTypeLocation;
    }

    /// <summary>Gets the read-only view of the declarations.</summary>
    public IDtdDeclarations Declarations { get; }

    /// <summary>Gets the options.</summary>
    public DtdValidationOptions Options { get; }

    /// <summary>Gets a value indicating whether the XML declaration says <c>standalone="yes"</c>.</summary>
    public bool IsStandalone { get; }

    /// <summary>Gets the system identifier of the document, <see langword="null"/> when unknown.</summary>
    public string? SystemId { get; }

    /// <summary>
    /// Gets the location of the document type declaration, <see langword="default"/> when unknown. It is where
    /// declaration-time checks report until per-declaration locations exist (#608), and the fallback of every report.
    /// </summary>
    public DocumentLocation DocumentTypeLocation { get; }

    /// <summary>Converts a position of the reader to a location in the document.</summary>
    /// <param name="position">The position.</param>
    /// <returns>The location with the system identifier of the document.</returns>
    public DocumentLocation Locate(in XmlPosition position) => new(SystemId, position.Line, position.Column);

    /// <summary>Reports one violation of a validity constraint or one other report.</summary>
    /// <param name="code">The code.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="location">The location of the violation.</param>
    /// <param name="argument">The offending item, for example a token or a name, or <see langword="null"/>.</param>
    public void Report(DtdValidityCode code, IssueSeverity severity, DocumentLocation location, string? argument = null)
    {
        Argument.ThrowIfNull(code);
        Argument.ThrowIfGreaterThan((int)severity, (int)IssueSeverity.Error);

        var message = argument is null ? code.Message : code.Message + " (" + argument + ")";
        _issues.Add(new ValidationIssue(code.Code, severity, message, OrDocumentType(location), code.Name));
    }

    /// <summary>Reports a violation of Standalone Document Declaration; the clause discriminates the four cases under one code.</summary>
    /// <param name="clause">The violated clause.</param>
    /// <param name="location">The location of the violation.</param>
    /// <param name="argument">The offending item, or <see langword="null"/>.</param>
    public void ReportStandalone(XmlStandaloneClause clause, DocumentLocation location, string? argument = null)
    {
        Argument.ThrowIfGreaterThan((int)clause, (int)XmlStandaloneClause.WhiteSpace);
        Argument.ThrowIfLessThan((int)clause, (int)XmlStandaloneClause.Defaults);

        var detail = argument is null ? "clause: " + clause : "clause: " + clause + ", " + argument;
        Report(DtdValidityCodes.XML5003, IssueSeverity.Error, location, detail);
    }

    /// <summary>Gets a value indicating whether <see cref="Stop"/> was called; the run ends and later issues are ignored.</summary>
    public bool IsStopped => _issues.IsStopped;

    /// <summary>Stops the run with a fatal status, for example a limit of <see cref="DtdLimitCodes"/>; validity issues never stop the run.</summary>
    /// <param name="code">The code of the fatal status.</param>
    /// <param name="location">The location where the status was found.</param>
    public void Stop(XmlErrorCode code, DocumentLocation location)
    {
        Argument.ThrowIfNull(code);

        _issues.Stop(
            new ValidationIssue(code.Code, IssueSeverity.Error, code.Message, OrDocumentType(location), code.Constraint)
        );
    }

    private DocumentLocation OrDocumentType(DocumentLocation location) =>
        location.Line == 0 ? DocumentTypeLocation : location;
}
