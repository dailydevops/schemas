namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>assert</c> element: fires when its test is false.</summary>
public sealed class SchematronAssert : SchematronAssertion
{
    internal SchematronAssert(
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
        : base(location, test, id, flags, role, severity, subject, diagnosticIds, propertyIds, message) { }
}
