#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas;

/// <summary>One finding of a validation or schema compilation. Immutable.</summary>
/// <remarks>Limitation: members throw until FND-09 lands.</remarks>
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
    ) => throw Stub.For("FND-09");

    /// <summary>Gets the stable diagnostic code.</summary>
    public DiagnosticCode Code => throw Stub.For("FND-09");

    /// <summary>Gets the severity.</summary>
    public IssueSeverity Severity => throw Stub.For("FND-09");

    /// <summary>Gets the message.</summary>
    public string Message => throw Stub.For("FND-09");

    /// <summary>Gets the location in the document.</summary>
    public DocumentLocation Location => throw Stub.For("FND-09");

    /// <summary>Gets the constraint identifier of the specification.</summary>
    public string? Constraint => throw Stub.For("FND-09");

    /// <inheritdoc />
    public override string ToString() => throw Stub.For("FND-09");
}
