namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>report</c> element: fires when its test is true.</summary>
public sealed class SchematronReport : SchematronAssertion
{
    internal SchematronReport(
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
