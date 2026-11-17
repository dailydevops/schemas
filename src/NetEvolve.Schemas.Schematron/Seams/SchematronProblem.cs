namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>A load, compile, or evaluation problem; converted to a <see cref="ValidationIssue"/> by the mapper.</summary>
internal readonly struct SchematronProblem
{
    internal SchematronProblem(
        DiagnosticCode code,
        string message,
        DocumentLocation location,
        string? constraint = null
    )
    {
        Code = code;
        Message = message;
        Location = location;
        Constraint = constraint;
    }

    internal DiagnosticCode Code { get; }

    internal string Message { get; }

    internal DocumentLocation Location { get; }

    internal string? Constraint { get; }
}
