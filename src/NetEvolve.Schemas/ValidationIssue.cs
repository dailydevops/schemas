namespace NetEvolve.Schemas;

using System.Globalization;
using NetEvolve.Arguments;

/// <summary>One finding of a validation or schema compilation. Immutable.</summary>
public sealed class ValidationIssue
{
    /// <summary>Initializes a new instance of the <see cref="ValidationIssue"/> class.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="message">The message.</param>
    /// <param name="location">The location in the document.</param>
    /// <param name="constraint">The constraint identifier of the specification, or <see langword="null"/>.</param>
    public ValidationIssue(
        DiagnosticCode code,
        IssueSeverity severity,
        string message,
        DocumentLocation location,
        string? constraint = null
    )
    {
        Argument.ThrowIfNull(message);
        Argument.ThrowIfGreaterThan((int)severity, (int)IssueSeverity.Error);

        Code = code;
        Severity = severity;
        Message = message;
        Location = location;
        Constraint = constraint;
    }

    /// <summary>Gets the stable diagnostic code.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the severity.</summary>
    public IssueSeverity Severity { get; }

    /// <summary>Gets the message.</summary>
    public string Message { get; }

    /// <summary>Gets the location in the document.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Gets the constraint identifier of the specification.</summary>
    public string? Constraint { get; }

    /// <summary>Returns <c>code severity location: message</c>.</summary>
    /// <returns>The text form.</returns>
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}: {3}", Code, Severity, Location, Message);
}
