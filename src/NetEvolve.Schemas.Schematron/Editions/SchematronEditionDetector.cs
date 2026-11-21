namespace NetEvolve.Schemas.Schematron.Editions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>Maps the value of <c>/sch:schema/@schematronEdition</c> to the edition whose semantics apply.</summary>
/// <remarks>
/// <para>
/// Design #106 Q11: an absent attribute and <c>2016</c> select the 2016 semantics, <c>2025</c> selects the 2025 semantics, and any
/// other value is the error <see cref="SchematronDiagnosticCodes.EditionUnknown"/>. The comparison is exact and ordinal: the value is
/// neither trimmed nor case-folded, so an empty value, <c>2025 </c>, and <c>2025.0</c> are unknown.
/// </para>
/// <para>
/// Limitation: the Xml package records positions for elements only, so the location of an unknown value is the start of the
/// <c>schema</c> element that carries the attribute.
/// </para>
/// </remarks>
internal static class SchematronEditionDetector
{
    /// <summary>The name of the attribute, without namespace, on the <c>schema</c> element.</summary>
    internal const string AttributeName = "schematronEdition";

    /// <summary>Maps the attribute value to an edition.</summary>
    /// <param name="text">The attribute value, or <see langword="null"/> when the attribute is absent.</param>
    /// <param name="edition">The edition; <see cref="SchematronEdition.Iso2016"/> when the value is unknown, so a caller that reports the problem can go on.</param>
    /// <returns><see langword="true"/> when the value is absent or known; <see langword="false"/> when it is unknown.</returns>
    internal static bool TryDetect(string? text, out SchematronEdition edition)
    {
        edition = string.Equals(text, "2025", StringComparison.Ordinal)
            ? SchematronEdition.Iso2025
            : SchematronEdition.Iso2016;

        return text is null
            || edition == SchematronEdition.Iso2025
            || string.Equals(text, "2016", StringComparison.Ordinal);
    }

    /// <summary>Gets the message of the problem for an unknown value.</summary>
    /// <param name="text">The unknown attribute value.</param>
    /// <returns>The message, naming the attribute and the value.</returns>
    internal static string UnknownMessage(string text)
    {
        Argument.ThrowIfNull(text);

        return "The " + AttributeName + " value '" + text + "' is not known.";
    }

    /// <summary>Detects the edition and fails for an unknown value.</summary>
    /// <param name="text">The attribute value, or <see langword="null"/> when the attribute is absent.</param>
    /// <param name="location">The location of the <c>schema</c> element.</param>
    /// <returns>The edition whose semantics apply.</returns>
    /// <exception cref="SchemaCompilationException">The value is unknown; the issue carries <see cref="SchematronDiagnosticCodes.EditionUnknown"/> and <paramref name="location"/>.</exception>
    internal static SchematronEdition Detect(string? text, DocumentLocation location)
    {
        if (TryDetect(text, out var edition))
        {
            return edition;
        }

        throw new SchemaCompilationException(
            new ValidationIssue[]
            {
                new(SchematronDiagnosticCodes.EditionUnknown, IssueSeverity.Error, UnknownMessage(text!), location),
            }
        );
    }
}
