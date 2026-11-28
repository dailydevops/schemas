namespace NetEvolve.Schemas.Schematron.Pipeline;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The join between the limits and the failures of the validator and those of the Xml package: the reader options that follow
/// <see cref="ValidationLimits"/>, and the issue of a failed read.
/// </summary>
/// <remarks>
/// A condition that has a core code (a limit) is reported with the core code, every other failure with its code of the Xml range
/// (<c>NES1000</c> to <c>NES1999</c>). The position of the issue is the one the reader found.
/// </remarks>
internal static class XmlIssues
{
    /// <summary>Creates the reader options of a read under the limits: depth, attributes, entities, and size.</summary>
    /// <param name="limits">The limits of the validator.</param>
    /// <returns>The options.</returns>
    internal static XmlReaderOptions Options(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        return XmlReaderOptions.Default.WithLimits(
            XmlReaderLimits
                .Default.WithMaxDocumentCharacters(limits.MaxInputSize)
                .WithMaxElementDepth(limits.MaxDepth)
                .WithMaxAttributesPerElement(limits.MaxAttributesPerElement)
                .WithMaxEntityExpansionCharacters(limits.MaxEntityExpansion)
                .WithMaxEntityDepth(limits.MaxEntityDepth)
        );
    }

    /// <summary>Creates the issue of a failed read.</summary>
    /// <param name="failure">The exception of the loader.</param>
    /// <param name="systemId">The system identifier of the document, or <see langword="null"/>.</param>
    /// <returns>The issue: an error at the position of the reader.</returns>
    internal static ValidationIssue Issue(XmlParseException failure, string? systemId)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (failure.Diagnostic is not { } diagnostic)
        {
            return new ValidationIssue(
                SchematronDiagnosticCodes.ModuleInvalid,
                IssueSeverity.Error,
                "The document is not well formed.",
                new DocumentLocation(systemId, 0, 0)
            );
        }

        var code = diagnostic.Code;
        return new ValidationIssue(
            code.CoreCode == default ? code.Code : code.CoreCode,
            IssueSeverity.Error,
            diagnostic.Message,
            new DocumentLocation(systemId, diagnostic.Position.Line, diagnostic.Position.Column),
            code.Constraint
        );
    }
}
