namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>Base of <c>assert</c> and <c>report</c>, which differ only in when they fire.</summary>
public abstract class SchematronAssertion : SchematronNode
{
    private protected SchematronAssertion(
        DocumentLocation location,
        string test,
        string? id,
        IReadOnlyList<string> flags,
        string? role,
        string? severity,
        string? subject,
        IReadOnlyList<string> diagnosticIds,
        IReadOnlyList<string> propertyIds,
        SchematronMessage message
    )
        : base(location)
    {
        Test = test;
        Id = id;
        Flags = flags;
        Role = role;
        Severity = severity;
        Subject = subject;
        DiagnosticIds = diagnosticIds;
        PropertyIds = propertyIds;
        Message = message;
    }

    /// <summary>Gets the raw <c>test</c> expression.</summary>
    public string Test { get; }

    /// <summary>Gets the <c>id</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Id { get; }

    /// <summary>Gets the <c>flag</c> tokens.</summary>
    public IReadOnlyList<string> Flags { get; }

    /// <summary>Gets the <c>role</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Role { get; }

    /// <summary>Gets the <c>severity</c> token as written, or <see langword="null"/> when absent.</summary>
    public string? Severity { get; }

    /// <summary>Gets the raw <c>subject</c> expression, or <see langword="null"/> when absent.</summary>
    public string? Subject { get; }

    /// <summary>Gets the ids of the referenced diagnostics.</summary>
    public IReadOnlyList<string> DiagnosticIds { get; }

    /// <summary>Gets the ids of the referenced properties.</summary>
    public IReadOnlyList<string> PropertyIds { get; }

    /// <summary>Gets the message.</summary>
    public SchematronMessage Message { get; }
}
