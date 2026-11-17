namespace NetEvolve.Schemas.Schematron.Diagnostics;

using System;

/// <summary>The failure of a contract stage that a later work package implements.</summary>
internal static class NotImplementedFailure
{
    /// <summary>Creates the exception carrying one <see cref="SchematronDiagnosticCodes.NotImplemented"/> issue.</summary>
    /// <param name="stage">The name of the stage that is still a stub.</param>
    /// <returns>The exception to throw.</returns>
    internal static SchemaCompilationException For(string stage) =>
        new(
            new ValidationIssue[]
            {
                new(
                    SchematronDiagnosticCodes.NotImplemented,
                    IssueSeverity.Error,
                    $"The Schematron stage '{stage}' is not implemented yet.",
                    new DocumentLocation(null, 0, 0)
                ),
            }
        );
}
