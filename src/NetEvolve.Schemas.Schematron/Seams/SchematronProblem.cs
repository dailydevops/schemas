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

    /// <summary>Returns the same problem at another location, as the element that holds the expression.</summary>
    /// <param name="location">The new location.</param>
    /// <returns>The relocated problem.</returns>
    internal SchematronProblem At(DocumentLocation location) => new(Code, Message, location, Constraint);

    internal DiagnosticCode Code { get; }

    internal string Message { get; }

    internal DocumentLocation Location { get; }

    internal string? Constraint { get; }
}
