namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>The outcome of validating a lexical form: valid, or the coded error, the failed facet, and the offset. Validation yields no value.</summary>
/// <remarks>Facets are checked in a fixed order: whitespace, lexical pattern, then the value-space facets. <see langword="default"/> is not valid; use <see cref="Valid"/>.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ValidationResult
{
    private ValidationResult(bool isValid, DatatypeError error, FacetKind failedFacet, int offset)
    {
        IsValid = isValid;
        Error = error;
        FailedFacet = failedFacet;
        Offset = offset;
    }

    /// <summary>Gets the result of a valid lexical form.</summary>
    public static ValidationResult Valid => new(true, DatatypeError.None, FacetKind.None, 0);

    /// <summary>Gets a value indicating whether the lexical form is valid.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the error, <see cref="DatatypeError.None"/> when valid or when a facet failed without a lexical error.</summary>
    public DatatypeError Error { get; }

    /// <summary>Gets the facet that failed, <see cref="FacetKind.None"/> when valid or when the lexical form itself is invalid.</summary>
    public FacetKind FailedFacet { get; }

    /// <summary>Gets the zero-based offset of the error in the lexical form, zero when valid.</summary>
    public int Offset { get; }

    /// <summary>Creates a result for an invalid lexical form.</summary>
    /// <param name="error">The error.</param>
    /// <param name="failedFacet">The failed facet, or <see cref="FacetKind.None"/>.</param>
    /// <param name="offset">The zero-based offset of the error in the lexical form.</param>
    /// <returns>The result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    public static ValidationResult Invalid(DatatypeError error, FacetKind failedFacet, int offset)
    {
        Argument.ThrowIfLessThan(offset, 0);

        return new(false, error, failedFacet, offset);
    }
}
